using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VersopayDatabase.Migrations
{
    public partial class SplitCpfCnpj_Safe : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ========= USUARIOS =========

            // 1) Remover índice antigo (se existir)
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Usuarios_CpfCnpj' AND object_id = OBJECT_ID(N'[dbo].[Usuarios]'))
BEGIN
    DROP INDEX [IX_Usuarios_CpfCnpj] ON [dbo].[Usuarios];
END
");

            // 2) Remover check antigo (se existir)
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Usuarios_CpfCnpj_Tipo' AND parent_object_id = OBJECT_ID(N'[dbo].[Usuarios]'))
BEGIN
    ALTER TABLE [dbo].[Usuarios] DROP CONSTRAINT [CK_Usuarios_CpfCnpj_Tipo];
END
");

            // 3) Se a coluna CpfCnpj existir e Cnpj não existir, renomeia
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'[dbo].[Usuarios]', N'CpfCnpj') IS NOT NULL
   AND COL_LENGTH(N'[dbo].[Usuarios]', N'Cnpj') IS NULL
BEGIN
    EXEC sp_rename N'[dbo].[Usuarios].[CpfCnpj]', N'Cnpj', 'COLUMN';
END
");

            // 4) Adicionar Cpf se não existir
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'[dbo].[Usuarios]', N'Cpf') IS NULL
BEGIN
    ALTER TABLE [dbo].[Usuarios] ADD [Cpf] NVARCHAR(11) NULL;
END
");

            // 5) Backfill consistente
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'[dbo].[Usuarios]', N'Cnpj') IS NOT NULL
   AND COL_LENGTH(N'[dbo].[Usuarios]', N'Cpf') IS NOT NULL
BEGIN
    UPDATE U
       SET U.Cpf = CASE WHEN LEN(U.Cnpj) = 11 THEN U.Cnpj ELSE U.Cpf END,
           U.Cnpj = CASE WHEN LEN(U.Cnpj) = 11 THEN NULL    ELSE U.Cnpj END
      FROM [dbo].[Usuarios] U
     WHERE U.TipoCadastro = 0; -- PF

    UPDATE U SET U.Cpf = NULL
      FROM [dbo].[Usuarios] U
     WHERE U.TipoCadastro = 1; -- PJ
END
");

            // 6) Índices únicos filtrados (se não existirem)
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Usuarios_Cpf' AND object_id = OBJECT_ID(N'[dbo].[Usuarios]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Usuarios_Cpf] ON [dbo].[Usuarios]([Cpf]) WHERE [Cpf] IS NOT NULL;
END
");
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Usuarios_Cnpj' AND object_id = OBJECT_ID(N'[dbo].[Usuarios]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Usuarios_Cnpj] ON [dbo].[Usuarios]([Cnpj]) WHERE [Cnpj] IS NOT NULL;
END
");

            // 7) Check constraint nova (se não existir)
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Usuarios_Cpf_Cnpj_Tipo' AND parent_object_id = OBJECT_ID(N'[dbo].[Usuarios]'))
BEGIN
    ALTER TABLE [dbo].[Usuarios] WITH NOCHECK ADD CONSTRAINT [CK_Usuarios_Cpf_Cnpj_Tipo]
    CHECK (
        (TipoCadastro IS NULL AND Cpf IS NULL AND Cnpj IS NULL)
        OR (TipoCadastro = 0 AND LEN(Cpf) = 11 AND Cnpj IS NULL)
        OR (TipoCadastro = 1 AND LEN(Cnpj) = 14 AND Cpf IS NULL)
    );
END
");

            // ========= KYCKYBS =========

            // 1) Dropar CpfCnpj (se existir)
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'[dbo].[KycKybs]', N'CpfCnpj') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[KycKybs] DROP COLUMN [CpfCnpj];
END
");

            // 2) Adicionar Cpf/Cnpj (se não existirem)
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'[dbo].[KycKybs]', N'Cpf') IS NULL
BEGIN
    ALTER TABLE [dbo].[KycKybs] ADD [Cpf] NVARCHAR(11) NULL;
END
");
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'[dbo].[KycKybs]', N'Cnpj') IS NULL
BEGIN
    ALTER TABLE [dbo].[KycKybs] ADD [Cnpj] NVARCHAR(14) NULL;
END
");

            // 3) Check de consistência (se não existir)
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_KycKyb_Cpf_Cnpj' AND parent_object_id = OBJECT_ID(N'[dbo].[KycKybs]'))
BEGIN
    ALTER TABLE [dbo].[KycKybs] WITH NOCHECK ADD CONSTRAINT [CK_KycKyb_Cpf_Cnpj]
    CHECK (
         (Cpf IS NULL AND Cnpj IS NULL)
      OR (LEN(Cpf) = 11 AND Cnpj IS NULL)
      OR (LEN(Cnpj) = 14 AND Cpf IS NULL)
    );
END
");

            // ========= FATURAMENTOS =========

            // 1) Criar tabela se não existir (layout novo)
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Faturamentos]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Faturamentos](
        [Id] INT NOT NULL IDENTITY,
        [Cpf] NVARCHAR(11) NULL,
        [Cnpj] NVARCHAR(14) NULL,
        [DataInicio] DATETIME2 NOT NULL,
        [DataFim] DATETIME2 NOT NULL,
        [VendasTotais] DECIMAL(18,2) NOT NULL DEFAULT(0),
        [VendasCartao] DECIMAL(18,2) NOT NULL DEFAULT(0),
        [VendasBoleto] DECIMAL(18,2) NOT NULL DEFAULT(0),
        [VendasPix] DECIMAL(18,2) NOT NULL DEFAULT(0),
        [Reserva] DECIMAL(18,2) NOT NULL DEFAULT(0),
        [VendasCanceladas] INT NOT NULL DEFAULT(0),
        [DiasSemVendas] INT NOT NULL DEFAULT(0),
        [AtualizadoEmUtc] DATETIME2 NOT NULL,
        CONSTRAINT [PK_Faturamentos] PRIMARY KEY ([Id])
    );
END
");

            // 2) Se existir e tiver CpfCnpj antigo, fazer split **com SQL dinâmico**
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Faturamentos]', 'U') IS NOT NULL
   AND COL_LENGTH(N'[dbo].[Faturamentos]', N'CpfCnpj') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[Faturamentos]', N'Cpf') IS NULL
        ALTER TABLE [dbo].[Faturamentos] ADD [Cpf] NVARCHAR(11) NULL;

    IF COL_LENGTH(N'[dbo].[Faturamentos]', N'Cnpj') IS NULL
        ALTER TABLE [dbo].[Faturamentos] ADD [Cnpj] NVARCHAR(14) NULL;

    DECLARE @dyn NVARCHAR(MAX) = N'
        UPDATE F
           SET F.Cpf  = CASE WHEN LEN(F.CpfCnpj) = 11 THEN F.CpfCnpj ELSE F.Cpf END,
               F.Cnpj = CASE WHEN LEN(F.CpfCnpj) = 14 THEN F.CpfCnpj ELSE F.Cnpj END
          FROM [dbo].[Faturamentos] F;

        ALTER TABLE [dbo].[Faturamentos] DROP COLUMN [CpfCnpj];
    ';
    EXEC sp_executesql @dyn;
END
");

            // 3) Índices (se não existirem)
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Faturamentos_Cpf' AND object_id = OBJECT_ID(N'[dbo].[Faturamentos]'))
BEGIN
    CREATE INDEX [IX_Faturamentos_Cpf] ON [dbo].[Faturamentos]([Cpf]);
END
");
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Faturamentos_Cnpj' AND object_id = OBJECT_ID(N'[dbo].[Faturamentos]'))
BEGIN
    CREATE INDEX [IX_Faturamentos_Cnpj] ON [dbo].[Faturamentos]([Cnpj]);
END
");
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Faturamentos_Cpf_Cnpj_DataInicio_DataFim' AND object_id = OBJECT_ID(N'[dbo].[Faturamentos]'))
BEGIN
    CREATE INDEX [IX_Faturamentos_Cpf_Cnpj_DataInicio_DataFim]
        ON [dbo].[Faturamentos]([Cpf], [Cnpj], [DataInicio], [DataFim]);
END
");

            // ========= TRANSFERENCIAS =========
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'[dbo].[Transferencias]', N'ExternalId') IS NULL
    ALTER TABLE [dbo].[Transferencias] ADD [ExternalId] NVARCHAR(80) NULL;
IF COL_LENGTH(N'[dbo].[Transferencias]', N'GatewayTransactionId') IS NULL
    ALTER TABLE [dbo].[Transferencias] ADD [GatewayTransactionId] NVARCHAR(80) NULL;
IF COL_LENGTH(N'[dbo].[Transferencias]', N'MetodoPagamento') IS NULL
    ALTER TABLE [dbo].[Transferencias] ADD [MetodoPagamento] INT NOT NULL DEFAULT(0);
");
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Transferencias_ExternalId' AND object_id = OBJECT_ID(N'[dbo].[Transferencias]'))
    CREATE INDEX [IX_Transferencias_ExternalId] ON [dbo].[Transferencias]([ExternalId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Transferencias_GatewayTransactionId' AND object_id = OBJECT_ID(N'[dbo].[Transferencias]'))
    CREATE INDEX [IX_Transferencias_GatewayTransactionId] ON [dbo].[Transferencias]([GatewayTransactionId]);
");

            // ========= PEDIDOS =========
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'[dbo].[Pedidos]', N'ExternalId') IS NULL
    ALTER TABLE [dbo].[Pedidos] ADD [ExternalId] NVARCHAR(80) NULL;
IF COL_LENGTH(N'[dbo].[Pedidos]', N'GatewayTransactionId') IS NULL
    ALTER TABLE [dbo].[Pedidos] ADD [GatewayTransactionId] NVARCHAR(80) NULL;
");
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pedidos_ExternalId' AND object_id = OBJECT_ID(N'[dbo].[Pedidos]'))
    CREATE INDEX [IX_Pedidos_ExternalId] ON [dbo].[Pedidos]([ExternalId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pedidos_GatewayTransactionId' AND object_id = OBJECT_ID(N'[dbo].[Pedidos]'))
    CREATE INDEX [IX_Pedidos_GatewayTransactionId] ON [dbo].[Pedidos]([GatewayTransactionId]);
");

            // ========= INBOUND WEBHOOK LOG =========
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[InboundWebhookLog]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InboundWebhookLog]
    (
        [Id] BIGINT NOT NULL IDENTITY(1,1) CONSTRAINT [PK_InboundWebhookLog] PRIMARY KEY,
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
        [TransferenciaId] INT NULL
    );

    CREATE UNIQUE INDEX [IX_InboundWebhookLog_EventKey]
        ON [dbo].[InboundWebhookLog]([EventKey]);
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ========= INBOUND WEBHOOK LOG =========
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[InboundWebhookLog]', 'U') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[InboundWebhookLog];
END
");

            // ========= PEDIDOS =========
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Pedidos]', 'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pedidos_ExternalId' AND object_id = OBJECT_ID(N'[dbo].[Pedidos]'))
        DROP INDEX [IX_Pedidos_ExternalId] ON [dbo].[Pedidos];

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pedidos_GatewayTransactionId' AND object_id = OBJECT_ID(N'[dbo].[Pedidos]'))
        DROP INDEX [IX_Pedidos_GatewayTransactionId] ON [dbo].[Pedidos];

    IF COL_LENGTH(N'[dbo].[Pedidos]', N'ExternalId') IS NOT NULL
        ALTER TABLE [dbo].[Pedidos] DROP COLUMN [ExternalId];

    IF COL_LENGTH(N'[dbo].[Pedidos]', N'GatewayTransactionId') IS NOT NULL
        ALTER TABLE [dbo].[Pedidos] DROP COLUMN [GatewayTransactionId];
END
");

            // ========= TRANSFERENCIAS =========
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Transferencias]', 'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Transferencias_ExternalId' AND object_id = OBJECT_ID(N'[dbo].[Transferencias]'))
        DROP INDEX [IX_Transferencias_ExternalId] ON [dbo].[Transferencias];

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Transferencias_GatewayTransactionId' AND object_id = OBJECT_ID(N'[dbo].[Transferencias]'))
        DROP INDEX [IX_Transferencias_GatewayTransactionId] ON [dbo].[Transferencias];

    IF COL_LENGTH(N'[dbo].[Transferencias]', N'ExternalId') IS NOT NULL
        ALTER TABLE [dbo].[Transferencias] DROP COLUMN [ExternalId];

    IF COL_LENGTH(N'[dbo].[Transferencias]', N'GatewayTransactionId') IS NOT NULL
        ALTER TABLE [dbo].[Transferencias] DROP COLUMN [GatewayTransactionId];

    -- Se quiser reverter MetodoPagamento também, descomente abaixo:
    -- IF COL_LENGTH(N'[dbo].[Transferencias]', N'MetodoPagamento') IS NOT NULL
    --     ALTER TABLE [dbo].[Transferencias] DROP COLUMN [MetodoPagamento];
END
");

            // ========= FATURAMENTOS =========
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Faturamentos]', 'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Faturamentos_Cpf_Cnpj_DataInicio_DataFim' AND object_id = OBJECT_ID(N'[dbo].[Faturamentos]'))
        DROP INDEX [IX_Faturamentos_Cpf_Cnpj_DataInicio_DataFim] ON [dbo].[Faturamentos];

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Faturamentos_Cpf' AND object_id = OBJECT_ID(N'[dbo].[Faturamentos]'))
        DROP INDEX [IX_Faturamentos_Cpf] ON [dbo].[Faturamentos];

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Faturamentos_Cnpj' AND object_id = OBJECT_ID(N'[dbo].[Faturamentos]'))
        DROP INDEX [IX_Faturamentos_Cnpj] ON [dbo].[Faturamentos];

    IF COL_LENGTH(N'[dbo].[Faturamentos]', N'CpfCnpj') IS NULL
        ALTER TABLE [dbo].[Faturamentos] ADD [CpfCnpj] NVARCHAR(14) NOT NULL DEFAULT(N'');

    UPDATE F SET F.CpfCnpj =
        CASE
            WHEN F.Cpf IS NOT NULL AND LEN(F.Cpf) = 11 THEN F.Cpf
            WHEN F.Cnpj IS NOT NULL AND LEN(F.Cnpj) = 14 THEN F.Cnpj
            ELSE F.CpfCnpj
        END
    FROM [dbo].[Faturamentos] F;

    IF COL_LENGTH(N'[dbo].[Faturamentos]', N'Cpf') IS NOT NULL
        ALTER TABLE [dbo].[Faturamentos] DROP COLUMN [Cpf];

    IF COL_LENGTH(N'[dbo].[Faturamentos]', N'Cnpj') IS NOT NULL
        ALTER TABLE [dbo].[Faturamentos] DROP COLUMN [Cnpj];

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Faturamentos_CpfCnpj' AND object_id = OBJECT_ID(N'[dbo].[Faturamentos]'))
        CREATE INDEX [IX_Faturamentos_CpfCnpj] ON [dbo].[Faturamentos]([CpfCnpj]);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Faturamentos_CpfCnpj_DataInicio_DataFim' AND object_id = OBJECT_ID(N'[dbo].[Faturamentos]'))
        CREATE INDEX [IX_Faturamentos_CpfCnpj_DataInicio_DataFim] ON [dbo].[Faturamentos]([CpfCnpj],[DataInicio],[DataFim]);
END
");

            // ========= KYCKYBS =========
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[KycKybs]', 'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_KycKyb_Cpf_Cnpj' AND parent_object_id = OBJECT_ID(N'[dbo].[KycKybs]'))
        ALTER TABLE [dbo].[KycKybs] DROP CONSTRAINT [CK_KycKyb_Cpf_Cnpj];

    IF COL_LENGTH(N'[dbo].[KycKybs]', N'CpfCnpj') IS NULL
        ALTER TABLE [dbo].[KycKybs] ADD [CpfCnpj] NVARCHAR(14) NOT NULL DEFAULT(N'');

    UPDATE K SET K.CpfCnpj =
        CASE
            WHEN K.Cpf IS NOT NULL AND LEN(K.Cpf) = 11 THEN K.Cpf
            WHEN K.Cnpj IS NOT NULL AND LEN(K.Cnpj) = 14 THEN K.Cnpj
            ELSE K.CpfCnpj
        END
    FROM [dbo].[KycKybs] K;

    IF COL_LENGTH(N'[dbo].[KycKybs]', N'Cpf') IS NOT NULL
        ALTER TABLE [dbo].[KycKybs] DROP COLUMN [Cpf];

    IF COL_LENGTH(N'[dbo].[KycKybs]', N'Cnpj') IS NOT NULL
        ALTER TABLE [dbo].[KycKybs] DROP COLUMN [Cnpj];
END
");

            // ========= USUARIOS =========
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Usuarios_Cpf_Cnpj_Tipo' AND parent_object_id = OBJECT_ID(N'[dbo].[Usuarios]'))
BEGIN
    ALTER TABLE [dbo].[Usuarios] DROP CONSTRAINT [CK_Usuarios_Cpf_Cnpj_Tipo];
END
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Usuarios_Cpf' AND object_id = OBJECT_ID(N'[dbo].[Usuarios]'))
    DROP INDEX [IX_Usuarios_Cpf] ON [dbo].[Usuarios];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Usuarios_Cnpj' AND object_id = OBJECT_ID(N'[dbo].[Usuarios]'))
    DROP INDEX [IX_Usuarios_Cnpj] ON [dbo].[Usuarios];
");

            migrationBuilder.Sql(@"
IF COL_LENGTH(N'[dbo].[Usuarios]', N'Cnpj') IS NOT NULL
   AND COL_LENGTH(N'[dbo].[Usuarios]', N'CpfCnpj') IS NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[Usuarios]', N'Cpf') IS NOT NULL
    BEGIN
        ALTER TABLE [dbo].[Usuarios] ADD [__TmpCpfCnpj] NVARCHAR(14) NULL;

        UPDATE U SET [__TmpCpfCnpj] =
            CASE
                WHEN U.Cpf IS NOT NULL AND LEN(U.Cpf) = 11 THEN U.Cpf
                WHEN U.Cnpj IS NOT NULL AND LEN(U.Cnpj) = 14 THEN U.Cnpj
                ELSE NULL
            END
        FROM [dbo].[Usuarios] U;

        EXEC sp_rename N'[dbo].[Usuarios].[Cnpj]', N'CpfCnpj', 'COLUMN';

        UPDATE U SET U.CpfCnpj = ISNULL([__TmpCpfCnpj], U.CpfCnpj) FROM [dbo].[Usuarios] U;

        ALTER TABLE [dbo].[Usuarios] DROP COLUMN [__TmpCpfCnpj];

        ALTER TABLE [dbo].[Usuarios] DROP COLUMN [Cpf];
    END
    ELSE
    BEGIN
        EXEC sp_rename N'[dbo].[Usuarios].[Cnpj]', N'CpfCnpj', 'COLUMN';
    END
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Usuarios_CpfCnpj' AND object_id = OBJECT_ID(N'[dbo].[Usuarios]'))
    CREATE UNIQUE INDEX [IX_Usuarios_CpfCnpj] ON [dbo].[Usuarios]([CpfCnpj]) WHERE [CpfCnpj] IS NOT NULL;
");
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Usuarios_CpfCnpj_Tipo' AND parent_object_id = OBJECT_ID(N'[dbo].[Usuarios]'))
BEGIN
    ALTER TABLE [dbo].[Usuarios] WITH NOCHECK ADD CONSTRAINT [CK_Usuarios_CpfCnpj_Tipo]
    CHECK (
        (TipoCadastro IS NULL AND [CpfCnpj] IS NULL)
        OR (TipoCadastro = 0 AND LEN([CpfCnpj]) = 11)
        OR (TipoCadastro = 1 AND LEN([CpfCnpj]) = 14)
    );
END
");
        }
    }
}
