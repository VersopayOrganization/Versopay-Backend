using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VersopayDatabase.Migrations
{
    public partial class SplitCpfCnpj_BackfillUsuarios : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cria a coluna somente se ela não existir; adiciona DEFAULT para viabilizar NOT NULL
            migrationBuilder.Sql(@"
                IF COL_LENGTH(N'[dbo].[Usuarios]', N'CadastroCompleto') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[Usuarios]
                      ADD [CadastroCompleto] BIT NOT NULL
                          CONSTRAINT [DF_Usuarios_CadastroCompleto] DEFAULT(0);
                END;

                -- Remove o DEFAULT para ficar alinhado ao modelo (opcional)
                DECLARE @df sysname, @sql nvarchar(max);
                SELECT @df = dc.name
                  FROM sys.default_constraints dc
                  JOIN sys.columns c ON c.default_object_id = dc.object_id
                 WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Usuarios]')
                   AND c.name = N'CadastroCompleto';
                IF @df IS NOT NULL
                BEGIN
                    SET @sql = N'ALTER TABLE [dbo].[Usuarios] DROP CONSTRAINT ' + QUOTENAME(@df) + N';';
                    EXEC(@sql);
                END;
                ");

            // ===== PRÉ-CLEANUP: USUARIOS =====
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Usuarios_CpfCnpj' AND object_id = OBJECT_ID(N'[dbo].[Usuarios]'))
                    DROP INDEX [IX_Usuarios_CpfCnpj] ON [dbo].[Usuarios];

                DECLARE @sql NVARCHAR(MAX);
                ;WITH ck AS (
                    SELECT sc.name AS ck_name
                    FROM sys.check_constraints sc
                    WHERE sc.parent_object_id = OBJECT_ID(N'[dbo].[Usuarios]')
                      AND sc.definition LIKE N'%CpfCnpj%'
                )
                SELECT @sql = STRING_AGG('ALTER TABLE [dbo].[Usuarios] DROP CONSTRAINT ' + QUOTENAME(ck_name) + ';', ' ')
                FROM ck;
                IF @sql IS NOT NULL EXEC sp_executesql @sql;

                IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Usuarios_CpfCnpj_Tipo' AND parent_object_id = OBJECT_ID(N'[dbo].[Usuarios]'))
                    ALTER TABLE [dbo].[Usuarios] DROP CONSTRAINT [CK_Usuarios_CpfCnpj_Tipo];

                IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Usuarios_Cpf_Cnpj_Tipo' AND parent_object_id = OBJECT_ID(N'[dbo].[Usuarios]'))
                    ALTER TABLE [dbo].[Usuarios] DROP CONSTRAINT [CK_Usuarios_Cpf_Cnpj_Tipo];
            ");

            // ===== USUARIOS =====
            migrationBuilder.Sql(@"
                IF COL_LENGTH(N'[dbo].[Usuarios]', N'CpfCnpj') IS NOT NULL
                    EXEC sp_rename N'[dbo].[Usuarios].[CpfCnpj]', N'Cnpj', 'COLUMN';

                IF COL_LENGTH(N'[dbo].[Usuarios]', N'Cpf') IS NULL
                    ALTER TABLE [dbo].[Usuarios] ADD [Cpf] NVARCHAR(11) NULL;

                IF COL_LENGTH(N'[dbo].[Usuarios]', N'Cnpj') IS NULL
                    ALTER TABLE [dbo].[Usuarios] ADD [Cnpj] NVARCHAR(14) NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE U
                   SET Cpf  = CASE WHEN LEN(U.Cnpj) = 11 THEN U.Cnpj ELSE U.Cpf END,
                       Cnpj = CASE WHEN LEN(U.Cnpj) = 11 THEN NULL    ELSE U.Cnpj END
                FROM [dbo].[Usuarios] U
                WHERE U.TipoCadastro = 0; -- PF
            ");

            migrationBuilder.Sql(@"
                UPDATE U
                   SET Cpf = NULL
                FROM [dbo].[Usuarios] U
                WHERE U.TipoCadastro = 1; -- PJ
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Usuarios_Cnpj' AND object_id = OBJECT_ID(N'[dbo].[Usuarios]'))
                    DROP INDEX [IX_Usuarios_Cnpj] ON [dbo].[Usuarios];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Usuarios_Cpf' AND object_id = OBJECT_ID(N'[dbo].[Usuarios]'))
                    DROP INDEX [IX_Usuarios_Cpf] ON [dbo].[Usuarios];
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Cnpj",
                table: "Usuarios",
                column: "Cnpj",
                unique: true,
                filter: "[Cnpj] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Cpf",
                table: "Usuarios",
                column: "Cpf",
                unique: true,
                filter: "[Cpf] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Usuarios_Cpf_Cnpj_Tipo",
                table: "Usuarios",
                sql: "((TipoCadastro IS NULL AND Cpf IS NULL AND Cnpj IS NULL) OR (TipoCadastro = 0 AND LEN(Cpf) = 11 AND Cnpj IS NULL) OR (TipoCadastro = 1 AND LEN(Cnpj) = 14 AND Cpf IS NULL))");

            // ===== KycKybs =====
            migrationBuilder.Sql(@"
                IF COL_LENGTH(N'[dbo].[KycKybs]', N'Cnpj') IS NULL
                    ALTER TABLE [dbo].[KycKybs] ADD [Cnpj] NVARCHAR(14) NULL;
                IF COL_LENGTH(N'[dbo].[KycKybs]', N'Cpf') IS NULL
                    ALTER TABLE [dbo].[KycKybs] ADD [Cpf] NVARCHAR(11) NULL;
                IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_KycKyb_Cpf_Cnpj' AND parent_object_id = OBJECT_ID(N'[dbo].[KycKybs]'))
                    ALTER TABLE [dbo].[KycKybs] DROP CONSTRAINT [CK_KycKyb_Cpf_Cnpj];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS(SELECT 1 FROM sys.columns WHERE name = N'CpfCnpj' AND object_id = OBJECT_ID(N'[dbo].[KycKybs]'))
                BEGIN
                    DECLARE @dyn NVARCHAR(MAX) = N'
                        UPDATE K SET
                            Cpf  = CASE WHEN LEN(K.CpfCnpj) = 11 THEN K.CpfCnpj ELSE K.Cpf END,
                            Cnpj = CASE WHEN LEN(K.CpfCnpj) = 14 THEN K.CpfCnpj ELSE K.Cnpj END
                        FROM [dbo].[KycKybs] K;
                        ALTER TABLE [dbo].[KycKybs] DROP COLUMN [CpfCnpj];
                    ';
                    EXEC sp_executesql @dyn;
                END
            ");

            migrationBuilder.AddCheckConstraint(
                name: "CK_KycKyb_Cpf_Cnpj",
                table: "KycKybs",
                sql: "((Cpf IS NULL AND Cnpj IS NULL) OR (LEN(Cpf) = 11 AND Cnpj IS NULL) OR (LEN(Cnpj) = 14 AND Cpf IS NULL))");

            // ===== Faturamentos =====
            migrationBuilder.Sql(@"
                IF COL_LENGTH(N'[dbo].[Faturamentos]', N'Cnpj') IS NULL
                    ALTER TABLE [dbo].[Faturamentos] ADD [Cnpj] NVARCHAR(14) NULL;
                IF COL_LENGTH(N'[dbo].[Faturamentos]', N'Cpf') IS NULL
                    ALTER TABLE [dbo].[Faturamentos] ADD [Cpf] NVARCHAR(11) NULL;
            ");

            migrationBuilder.Sql(@"
                IF EXISTS(SELECT 1 FROM sys.columns WHERE name = N'CpfCnpj' AND object_id = OBJECT_ID(N'[dbo].[Faturamentos]'))
                BEGIN
                    DECLARE @dyn2 NVARCHAR(MAX) = N'
                        UPDATE F SET
                            Cpf  = CASE WHEN LEN(F.CpfCnpj) = 11 THEN F.CpfCnpj ELSE F.Cpf END,
                            Cnpj = CASE WHEN LEN(F.CpfCnpj) = 14 THEN F.CpfCnpj ELSE F.Cnpj END
                        FROM [dbo].[Faturamentos] F;
                        ALTER TABLE [dbo].[Faturamentos] DROP COLUMN [CpfCnpj];
                    ';
                    EXEC sp_executesql @dyn2;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Faturamentos_CpfCnpj' AND object_id = OBJECT_ID(N'[dbo].[Faturamentos]'))
                    DROP INDEX [IX_Faturamentos_CpfCnpj] ON [dbo].[Faturamentos];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Faturamentos_CpfCnpj_DataInicio_DataFim' AND object_id = OBJECT_ID(N'[dbo].[Faturamentos]'))
                    DROP INDEX [IX_Faturamentos_CpfCnpj_DataInicio_DataFim] ON [dbo].[Faturamentos];

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Faturamentos_Cnpj' AND object_id = OBJECT_ID(N'[dbo].[Faturamentos]'))
                    DROP INDEX [IX_Faturamentos_Cnpj] ON [dbo].[Faturamentos];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Faturamentos_Cpf' AND object_id = OBJECT_ID(N'[dbo].[Faturamentos]'))
                    DROP INDEX [IX_Faturamentos_Cpf] ON [dbo].[Faturamentos];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Faturamentos_Cpf_Cnpj_DataInicio_DataFim' AND object_id = OBJECT_ID(N'[dbo].[Faturamentos]'))
                    DROP INDEX [IX_Faturamentos_Cpf_Cnpj_DataInicio_DataFim] ON [dbo].[Faturamentos];
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Faturamentos_Cnpj",
                table: "Faturamentos",
                column: "Cnpj");

            migrationBuilder.CreateIndex(
                name: "IX_Faturamentos_Cpf",
                table: "Faturamentos",
                column: "Cpf");

            migrationBuilder.CreateIndex(
                name: "IX_Faturamentos_Cpf_Cnpj_DataInicio_DataFim",
                table: "Faturamentos",
                columns: new[] { "Cpf", "Cnpj", "DataInicio", "DataFim" });

            // ===== Transferencias/Pedidos =====
            migrationBuilder.Sql(@"
                IF COL_LENGTH(N'[dbo].[Transferencias]', N'ExternalId') IS NULL
                    ALTER TABLE [dbo].[Transferencias] ADD [ExternalId] NVARCHAR(80) NULL;
                IF COL_LENGTH(N'[dbo].[Transferencias]', N'GatewayTransactionId') IS NULL
                    ALTER TABLE [dbo].[Transferencias] ADD [GatewayTransactionId] NVARCHAR(80) NULL;
                IF COL_LENGTH(N'[dbo].[Transferencias]', N'MetodoPagamento') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[Transferencias] ADD [MetodoPagamento] INT NOT NULL CONSTRAINT DF_Transferencias_MetodoPagamento DEFAULT(0);
                    UPDATE [dbo].[Transferencias] SET [MetodoPagamento] = ISNULL([MetodoPagamento], 0);
                END

                IF COL_LENGTH(N'[dbo].[Pedidos]', N'ExternalId') IS NULL
                    ALTER TABLE [dbo].[Pedidos] ADD [ExternalId] NVARCHAR(80) NULL;
                IF COL_LENGTH(N'[dbo].[Pedidos]', N'GatewayTransactionId') IS NULL
                    ALTER TABLE [dbo].[Pedidos] ADD [GatewayTransactionId] NVARCHAR(80) NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Transferencias_ExternalId' AND object_id = OBJECT_ID(N'[dbo].[Transferencias]'))
                    CREATE INDEX [IX_Transferencias_ExternalId] ON [dbo].[Transferencias]([ExternalId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Transferencias_GatewayTransactionId' AND object_id = OBJECT_ID(N'[dbo].[Transferencias]'))
                    CREATE INDEX [IX_Transferencias_GatewayTransactionId] ON [dbo].[Transferencias]([GatewayTransactionId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pedidos_ExternalId' AND object_id = OBJECT_ID(N'[dbo].[Pedidos]'))
                    CREATE INDEX [IX_Pedidos_ExternalId] ON [dbo].[Pedidos]([ExternalId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pedidos_GatewayTransactionId' AND object_id = OBJECT_ID(N'[dbo].[Pedidos]'))
                    CREATE INDEX [IX_Pedidos_GatewayTransactionId] ON [dbo].[Pedidos]([GatewayTransactionId]);
            ");

            // ===== InboundWebhookLog (tabela/índice condicional) =====
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[dbo].[InboundWebhookLog]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[InboundWebhookLog](
                        [Id] BIGINT NOT NULL IDENTITY(1,1),
                        [Provedor] INT NOT NULL,
                        [Evento] INT NOT NULL,
                        [EventKey] NVARCHAR(180) NOT NULL,
                        [TransactionId] NVARCHAR(80) NULL,
                        [ExternalId] NVARCHAR(80) NULL,
                        [RequestNumber] NVARCHAR(80) NULL,
                        [Status] NVARCHAR(40) NULL,
                        [TipoTransacao] NVARCHAR(20) NULL,
                        [Valor] DECIMAL(18,2) NULL,
                        [Fee] DECIMAL(18,2) NULL,
                        [NetAmount] DECIMAL(18,2) NULL,
                        [DebtorName] NVARCHAR(MAX) NULL,
                        [DebtorDocument] NVARCHAR(MAX) NULL,
                        [Ispb] NVARCHAR(MAX) NULL,
                        [NomeRecebedor] NVARCHAR(MAX) NULL,
                        [CpfRecebedor] NVARCHAR(MAX) NULL,
                        [DataEventoUtc] DATETIME2 NULL,
                        [SourceIp] NVARCHAR(MAX) NOT NULL,
                        [HeadersJson] NVARCHAR(MAX) NOT NULL,
                        [PayloadJson] NVARCHAR(MAX) NOT NULL,
                        [ReceivedAtUtc] DATETIME2 NOT NULL,
                        [ProcessedAtUtc] DATETIME2 NULL,
                        [ProcessingStatus] INT NOT NULL,
                        [ProcessingError] NVARCHAR(MAX) NULL,
                        [PedidoId] INT NULL,
                        [TransferenciaId] INT NULL,
                        CONSTRAINT [PK_InboundWebhookLog] PRIMARY KEY ([Id])
                    );
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InboundWebhookLog_EventKey' AND object_id = OBJECT_ID(N'[dbo].[InboundWebhookLog]'))
                    CREATE UNIQUE INDEX [IX_InboundWebhookLog_EventKey] ON [dbo].[InboundWebhookLog]([EventKey]);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove a coluna apenas se existir
            migrationBuilder.Sql(@"
                IF COL_LENGTH(N'[dbo].[Usuarios]', N'CadastroCompleto') IS NOT NULL
                BEGIN
                    -- Garante dropar default constraint se houver
                    DECLARE @df sysname, @sql nvarchar(max);
                    SELECT @df = dc.name
                      FROM sys.default_constraints dc
                      JOIN sys.columns c ON c.default_object_id = dc.object_id
                     WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Usuarios]')
                       AND c.name = N'CadastroCompleto';
                    IF @df IS NOT NULL
                    BEGIN
                        SET @sql = N'ALTER TABLE [dbo].[Usuarios] DROP CONSTRAINT ' + QUOTENAME(@df) + N';';
                        EXEC(@sql);
                    END;

                    ALTER TABLE [dbo].[Usuarios] DROP COLUMN [CadastroCompleto];
                END;
                ");

            // ===== InboundWebhookLog (drop condicional) =====
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InboundWebhookLog_EventKey' AND object_id = OBJECT_ID(N'[dbo].[InboundWebhookLog]'))
                    DROP INDEX [IX_InboundWebhookLog_EventKey] ON [dbo].[InboundWebhookLog];

                IF OBJECT_ID(N'[dbo].[InboundWebhookLog]', N'U') IS NOT NULL
                    DROP TABLE [dbo].[InboundWebhookLog];
            ");

            // ===== Transferencias/Pedidos (índices/colunas condicionalmente) =====
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Transferencias_ExternalId' AND object_id = OBJECT_ID(N'[dbo].[Transferencias]'))
                    DROP INDEX [IX_Transferencias_ExternalId] ON [dbo].[Transferencias];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Transferencias_GatewayTransactionId' AND object_id = OBJECT_ID(N'[dbo].[Transferencias]'))
                    DROP INDEX [IX_Transferencias_GatewayTransactionId] ON [dbo].[Transferencias];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pedidos_ExternalId' AND object_id = OBJECT_ID(N'[dbo].[Pedidos]'))
                    DROP INDEX [IX_Pedidos_ExternalId] ON [dbo].[Pedidos];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pedidos_GatewayTransactionId' AND object_id = OBJECT_ID(N'[dbo].[Pedidos]'))
                    DROP INDEX [IX_Pedidos_GatewayTransactionId] ON [dbo].[Pedidos];

                IF COL_LENGTH(N'[dbo].[Transferencias]', N'ExternalId') IS NOT NULL
                    ALTER TABLE [dbo].[Transferencias] DROP COLUMN [ExternalId];
                IF COL_LENGTH(N'[dbo].[Transferencias]', N'GatewayTransactionId') IS NOT NULL
                    ALTER TABLE [dbo].[Transferencias] DROP COLUMN [GatewayTransactionId];
                IF COL_LENGTH(N'[dbo].[Transferencias]', N'MetodoPagamento') IS NOT NULL
                BEGIN
                    DECLARE @dcname sysname;
                    SELECT @dcname = dc.name
                    FROM sys.default_constraints dc
                    JOIN sys.columns c ON c.default_object_id = dc.object_id
                    WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Transferencias]')
                      AND c.name = N'MetodoPagamento';
                    IF @dcname IS NOT NULL
                        EXEC('ALTER TABLE [dbo].[Transferencias] DROP CONSTRAINT ' + QUOTENAME(@dcname) + ';');
                    ALTER TABLE [dbo].[Transferencias] DROP COLUMN [MetodoPagamento];
                END

                IF COL_LENGTH(N'[dbo].[Pedidos]', N'ExternalId') IS NOT NULL
                    ALTER TABLE [dbo].[Pedidos] DROP COLUMN [ExternalId];
                IF COL_LENGTH(N'[dbo].[Pedidos]', N'GatewayTransactionId') IS NOT NULL
                    ALTER TABLE [dbo].[Pedidos] DROP COLUMN [GatewayTransactionId];
            ");

            // ===== Faturamentos =====
            migrationBuilder.DropIndex(name: "IX_Faturamentos_Cnpj", table: "Faturamentos");
            migrationBuilder.DropIndex(name: "IX_Faturamentos_Cpf", table: "Faturamentos");
            migrationBuilder.DropIndex(name: "IX_Faturamentos_Cpf_Cnpj_DataInicio_DataFim", table: "Faturamentos");

            migrationBuilder.AddColumn<string>(
                name: "CpfCnpj",
                table: "Faturamentos",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE F SET CpfCnpj =
                    COALESCE(
                        CASE WHEN LEN(F.Cpf) = 11 THEN F.Cpf END,
                        CASE WHEN LEN(F.Cnpj) = 14 THEN F.Cnpj END,
                        F.CpfCnpj
                    )
                FROM [dbo].[Faturamentos] F;
            ");

            migrationBuilder.DropColumn(name: "Cpf", table: "Faturamentos");
            migrationBuilder.DropColumn(name: "Cnpj", table: "Faturamentos");

            migrationBuilder.CreateIndex(
                name: "IX_Faturamentos_CpfCnpj",
                table: "Faturamentos",
                column: "CpfCnpj");

            migrationBuilder.CreateIndex(
                name: "IX_Faturamentos_CpfCnpj_DataInicio_DataFim",
                table: "Faturamentos",
                columns: new[] { "CpfCnpj", "DataInicio", "DataFim" });

            // ===== KycKybs =====
            migrationBuilder.DropCheckConstraint(name: "CK_KycKyb_Cpf_Cnpj", table: "KycKybs");

            migrationBuilder.AddColumn<string>(
                name: "CpfCnpj",
                table: "KycKybs",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE K SET CpfCnpj =
                    COALESCE(
                        CASE WHEN LEN(K.Cpf) = 11 THEN K.Cpf END,
                        CASE WHEN LEN(K.Cnpj) = 14 THEN K.Cnpj END,
                        K.CpfCnpj
                    )
                FROM [dbo].[KycKybs] K;
            ");

            migrationBuilder.DropColumn(name: "Cpf", table: "KycKybs");
            migrationBuilder.DropColumn(name: "Cnpj", table: "KycKybs");

            // ===== Usuarios =====
            migrationBuilder.DropIndex(name: "IX_Usuarios_Cnpj", table: "Usuarios");
            migrationBuilder.DropIndex(name: "IX_Usuarios_Cpf", table: "Usuarios");
            migrationBuilder.DropCheckConstraint(name: "CK_Usuarios_Cpf_Cnpj_Tipo", table: "Usuarios");

            migrationBuilder.Sql(@"
                IF COL_LENGTH(N'[dbo].[Usuarios]', N'Cnpj') IS NOT NULL
                    EXEC sp_rename N'[dbo].[Usuarios].[Cnpj]', N'CpfCnpj', 'COLUMN';
            ");

            migrationBuilder.Sql(@"
                UPDATE U
                   SET CpfCnpj = COALESCE(
                        CASE WHEN LEN(U.Cpf) = 11 THEN U.Cpf END,
                        U.CpfCnpj
                   )
                FROM [dbo].[Usuarios] U;
            ");

            migrationBuilder.DropColumn(name: "Cpf", table: "Usuarios");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CpfCnpj",
                table: "Usuarios",
                column: "CpfCnpj",
                unique: true,
                filter: "[CpfCnpj] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Usuarios_CpfCnpj_Tipo",
                table: "Usuarios",
                sql: "((TipoCadastro IS NULL AND [CpfCnpj] IS NULL) OR (TipoCadastro = 0 AND LEN([CpfCnpj]) = 11) OR (TipoCadastro = 1 AND LEN([CpfCnpj]) = 14))");
        }
    }
}
