using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VersopayDatabase.Migrations
{
    public partial class AddPropCadastrCompletoInTabelUsuario : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ------------------------------------------------------------
            // GARANTE TABELAS BÁSICAS QUANDO O BANCO ESTÁ ZERADO
            // ------------------------------------------------------------

            // USUARIOS (esqueleto mínimo caso não exista)
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Usuarios]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Usuarios](
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Usuarios] PRIMARY KEY,
        [TipoCadastro] INT NULL,
        [CpfCnpj] NVARCHAR(14) NULL,
        [Cpf] NVARCHAR(11) NULL,
        [Cnpj] NVARCHAR(14) NULL
    );
END
");

            // KYCKYBS (esqueleto mínimo caso não exista)
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[KycKybs]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[KycKybs](
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_KycKybs] PRIMARY KEY,
        [CpfCnpj] NVARCHAR(14) NULL,
        [Cpf] NVARCHAR(11) NULL,
        [Cnpj] NVARCHAR(14) NULL
    );
END
");

            // FATURAMENTOS (cria layout completo caso não exista)
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Faturamentos]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Faturamentos](
        [Id] INT NOT NULL IDENTITY(1,1) CONSTRAINT [PK_Faturamentos] PRIMARY KEY,
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
        [AtualizadoEmUtc] DATETIME2 NOT NULL
    );
END
");

            // ------------------------------------------------------------
            // USUARIOS (1/4): garantir colunas novas
            // ------------------------------------------------------------
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Usuarios]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[Usuarios]', N'Cpf') IS NULL
        ALTER TABLE [dbo].[Usuarios] ADD [Cpf] NVARCHAR(11) NULL;
    IF COL_LENGTH(N'[dbo].[Usuarios]', N'Cnpj') IS NULL
        ALTER TABLE [dbo].[Usuarios] ADD [Cnpj] NVARCHAR(14) NULL;
END
");

            // ------------------------------------------------------------
            // USUARIOS (2/4): backfill + soltar deps + drop CpfCnpj
            // ------------------------------------------------------------
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Usuarios]', N'U') IS NOT NULL
   AND COL_LENGTH(N'[dbo].[Usuarios]', N'CpfCnpj') IS NOT NULL
BEGIN
    DECLARE @sql NVARCHAR(MAX) = N'
        UPDATE U
           SET Cpf  = CASE WHEN LEN(U.CpfCnpj)=11 AND (U.Cpf  IS NULL OR U.Cpf  = '''') THEN U.CpfCnpj ELSE U.Cpf  END,
               Cnpj = CASE WHEN LEN(U.CpfCnpj)=14 AND (U.Cnpj IS NULL OR U.Cnpj = '''') THEN U.CpfCnpj ELSE U.Cnpj END
          FROM [dbo].[Usuarios] U;';
    EXEC sp_executesql @sql;

    -- Índices que tocam CpfCnpj
    DECLARE @dropIdx NVARCHAR(MAX);
    ;WITH idx AS (
        SELECT i.name AS idx_name
          FROM sys.indexes i
          JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
          JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
         WHERE i.object_id = OBJECT_ID(N'[dbo].[Usuarios]')
           AND c.name = N'CpfCnpj'
           AND i.name IS NOT NULL
    )
    SELECT @dropIdx = STRING_AGG('DROP INDEX ' + QUOTENAME(idx_name) + ' ON [dbo].[Usuarios];', ' ')
      FROM idx;
    IF @dropIdx IS NOT NULL EXEC sp_executesql @dropIdx;

    -- DEFAULT em CpfCnpj (se houver)
    DECLARE @df sysname, @sqlDrop NVARCHAR(MAX);
    SELECT @df = dc.name
      FROM sys.default_constraints dc
      JOIN sys.columns c ON c.object_id=dc.parent_object_id AND c.column_id=dc.parent_column_id
     WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Usuarios]')
       AND c.name = N'CpfCnpj';
    IF @df IS NOT NULL
    BEGIN
        SET @sqlDrop = N'ALTER TABLE [dbo].[Usuarios] DROP CONSTRAINT ' + QUOTENAME(@df) + N';';
        EXEC sp_executesql @sqlDrop;
    END

    -- CHECKs que mencionem CpfCnpj
    DECLARE @dropCk NVARCHAR(MAX);
    ;WITH ck AS (
        SELECT sc.name AS ck_name
          FROM sys.check_constraints sc
         WHERE sc.parent_object_id = OBJECT_ID(N'[dbo].[Usuarios]')
           AND sc.definition LIKE N'%CpfCnpj%'
    )
    SELECT @dropCk = STRING_AGG('ALTER TABLE [dbo].[Usuarios] DROP CONSTRAINT ' + QUOTENAME(ck_name) + ';', ' ')
      FROM ck;
    IF @dropCk IS NOT NULL EXEC sp_executesql @dropCk;

    -- drop coluna
    ALTER TABLE [dbo].[Usuarios] DROP COLUMN [CpfCnpj];
END
");

            // ------------------------------------------------------------
            // USUARIOS (3/4): índices filtrados
            // ------------------------------------------------------------
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Usuarios]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[Usuarios]', N'Cpf') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Usuarios_Cpf' AND object_id = OBJECT_ID(N'[dbo].[Usuarios]'))
        EXEC sp_executesql N'CREATE UNIQUE INDEX [IX_Usuarios_Cpf] ON [dbo].[Usuarios]([Cpf]) WHERE [Cpf] IS NOT NULL;';

    IF COL_LENGTH(N'[dbo].[Usuarios]', N'Cnpj') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Usuarios_Cnpj' AND object_id = OBJECT_ID(N'[dbo].[Usuarios]'))
        EXEC sp_executesql N'CREATE UNIQUE INDEX [IX_Usuarios_Cnpj] ON [dbo].[Usuarios]([Cnpj]) WHERE [Cnpj] IS NOT NULL;';
END
");

            // ------------------------------------------------------------
            // USUARIOS (4/4): check constraint PF/PJ
            // ------------------------------------------------------------
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Usuarios]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Usuarios_Cpf_Cnpj_Tipo' AND parent_object_id = OBJECT_ID(N'[dbo].[Usuarios]'))
        EXEC('ALTER TABLE [dbo].[Usuarios] DROP CONSTRAINT [CK_Usuarios_Cpf_Cnpj_Tipo];');

    DECLARE @sqlCheck NVARCHAR(MAX) = N'
IF COL_LENGTH(''[dbo].[Usuarios]'', ''Cpf'') IS NOT NULL
   AND COL_LENGTH(''[dbo].[Usuarios]'', ''Cnpj'') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Usuarios] WITH NOCHECK
      ADD CONSTRAINT [CK_Usuarios_Cpf_Cnpj_Tipo]
      CHECK (
             (TipoCadastro IS NULL AND Cpf IS NULL AND Cnpj IS NULL)
          OR (TipoCadastro = 0 AND LEN(Cpf) = 11 AND Cnpj IS NULL)
          OR (TipoCadastro = 1 AND LEN(Cnpj) = 14 AND Cpf IS NULL)
      );
END;';
    EXEC sp_executesql @sqlCheck;
END
");

            // ------------------------------------------------------------
            // KYCKYBS: add + backfill + drop legada + check
            // ------------------------------------------------------------
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[KycKybs]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[KycKybs]', N'Cpf') IS NULL
        ALTER TABLE [dbo].[KycKybs] ADD [Cpf] NVARCHAR(11) NULL;
    IF COL_LENGTH(N'[dbo].[KycKybs]', N'Cnpj') IS NULL
        ALTER TABLE [dbo].[KycKybs] ADD [Cnpj] NVARCHAR(14) NULL;

    IF COL_LENGTH(N'[dbo].[KycKybs]', N'CpfCnpj') IS NOT NULL
    BEGIN
        DECLARE @sql NVARCHAR(MAX) = N'
            UPDATE K
               SET Cpf  = CASE WHEN LEN(K.CpfCnpj)=11 AND (K.Cpf  IS NULL OR K.Cpf  = '''') THEN K.CpfCnpj ELSE K.Cpf  END,
                   Cnpj = CASE WHEN LEN(K.CpfCnpj)=14 AND (K.Cnpj IS NULL OR K.Cnpj = '''') THEN K.CpfCnpj ELSE K.Cnpj END
              FROM [dbo].[KycKybs] K;';
        EXEC sp_executesql @sql;

        -- solta índices/default/checks que toquem CpfCnpj e dropar coluna
        DECLARE @dropIdx NVARCHAR(MAX), @df sysname, @sqlDrop NVARCHAR(MAX);
        ;WITH idx AS (
            SELECT i.name AS idx_name
              FROM sys.indexes i
              JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
              JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
             WHERE i.object_id = OBJECT_ID(N'[dbo].[KycKybs]')
               AND c.name = N'CpfCnpj'
               AND i.name IS NOT NULL
        )
        SELECT @dropIdx = STRING_AGG('DROP INDEX ' + QUOTENAME(idx_name) + ' ON [dbo].[KycKybs];', ' ')
          FROM idx;
        IF @dropIdx IS NOT NULL EXEC sp_executesql @dropIdx;

        SELECT @df = dc.name
          FROM sys.default_constraints dc
          JOIN sys.columns c ON c.object_id=dc.parent_object_id AND c.column_id=dc.parent_column_id
         WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[KycKybs]')
           AND c.name = N'CpfCnpj';
        IF @df IS NOT NULL
        BEGIN
            SET @sqlDrop = N'ALTER TABLE [dbo].[KycKybs] DROP CONSTRAINT ' + QUOTENAME(@df) + N';';
            EXEC sp_executesql @sqlDrop;
        END

        DECLARE @dropCk NVARCHAR(MAX);
        ;WITH ck AS (
            SELECT sc.name AS ck_name
              FROM sys.check_constraints sc
             WHERE sc.parent_object_id = OBJECT_ID(N'[dbo].[KycKybs]')
               AND sc.definition LIKE N'%CpfCnpj%'
        )
        SELECT @dropCk = STRING_AGG('ALTER TABLE [dbo].[KycKybs] DROP CONSTRAINT ' + QUOTENAME(ck_name) + ';', ' ')
          FROM ck;
        IF @dropCk IS NOT NULL EXEC sp_executesql @dropCk;

        ALTER TABLE [dbo].[KycKybs] DROP COLUMN [CpfCnpj];
    END;

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_KycKyb_Cpf_Cnpj' AND parent_object_id = OBJECT_ID(N'[dbo].[KycKybs]'))
        ALTER TABLE [dbo].[KycKybs] DROP CONSTRAINT [CK_KycKyb_Cpf_Cnpj];

    IF COL_LENGTH(N'[dbo].[KycKybs]', N'Cpf') IS NOT NULL
       AND COL_LENGTH(N'[dbo].[KycKybs]', N'Cnpj') IS NOT NULL
        EXEC sp_executesql N'
            ALTER TABLE [dbo].[KycKybs] WITH NOCHECK
              ADD CONSTRAINT [CK_KycKyb_Cpf_Cnpj]
              CHECK ( (Cpf IS NULL AND Cnpj IS NULL) OR (LEN(Cpf)=11 AND Cnpj IS NULL) OR (LEN(Cnpj)=14 AND Cpf IS NULL) );
        ';
END
");

            // ------------------------------------------------------------
            // FATURAMENTOS: agora é seguro mexer
            // ------------------------------------------------------------
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Faturamentos]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[Faturamentos]', N'Cpf') IS NULL
        ALTER TABLE [dbo].[Faturamentos] ADD [Cpf] NVARCHAR(11) NULL;
    IF COL_LENGTH(N'[dbo].[Faturamentos]', N'Cnpj') IS NULL
        ALTER TABLE [dbo].[Faturamentos] ADD [Cnpj] NVARCHAR(14) NULL;
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Faturamentos]', N'U') IS NOT NULL
   AND COL_LENGTH(N'[dbo].[Faturamentos]', N'CpfCnpj') IS NOT NULL
BEGIN
    DECLARE @sql NVARCHAR(MAX) = N'
        UPDATE F
           SET Cpf  = CASE WHEN LEN(F.CpfCnpj)=11 AND (F.Cpf  IS NULL OR F.Cpf  = '''') THEN F.CpfCnpj ELSE F.Cpf  END,
               Cnpj = CASE WHEN LEN(F.CpfCnpj)=14 AND (F.Cnpj IS NULL OR F.Cnpj = '''') THEN F.CpfCnpj ELSE F.Cnpj END
          FROM [dbo].[Faturamentos] F;';
    EXEC sp_executesql @sql;

    -- largar índices/default/checks ligados a CpfCnpj e dropar coluna
    DECLARE @dropIdx NVARCHAR(MAX), @df sysname, @sqlDrop NVARCHAR(MAX);

    ;WITH idx AS (
        SELECT i.name AS idx_name
          FROM sys.indexes i
          JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
          JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
         WHERE i.object_id = OBJECT_ID(N'[dbo].[Faturamentos]')
           AND c.name = N'CpfCnpj'
           AND i.name IS NOT NULL
    )
    SELECT @dropIdx = STRING_AGG('DROP INDEX ' + QUOTENAME(idx_name) + ' ON [dbo].[Faturamentos];', ' ')
      FROM idx;
    IF @dropIdx IS NOT NULL EXEC sp_executesql @dropIdx;

    SELECT @df = dc.name
      FROM sys.default_constraints dc
      JOIN sys.columns c ON c.object_id=dc.parent_object_id AND c.column_id=dc.parent_column_id
     WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Faturamentos]')
       AND c.name = N'CpfCnpj';
    IF @df IS NOT NULL
    BEGIN
        SET @sqlDrop = N'ALTER TABLE [dbo].[Faturamentos] DROP CONSTRAINT ' + QUOTENAME(@df) + N';';
        EXEC sp_executesql @sqlDrop;
    END

    DECLARE @dropCk NVARCHAR(MAX);
    ;WITH ck AS (
        SELECT sc.name AS ck_name
          FROM sys.check_constraints sc
         WHERE sc.parent_object_id = OBJECT_ID(N'[dbo].[Faturamentos]')
           AND sc.definition LIKE N'%CpfCnpj%'
    )
    SELECT @dropCk = STRING_AGG('ALTER TABLE [dbo].[Faturamentos] DROP CONSTRAINT ' + QUOTENAME(ck_name) + ';', ' ')
      FROM ck;
    IF @dropCk IS NOT NULL EXEC sp_executesql @dropCk;

    ALTER TABLE [dbo].[Faturamentos] DROP COLUMN [CpfCnpj];
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Faturamentos]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[Faturamentos]', N'Cpf') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Faturamentos_Cpf' AND object_id = OBJECT_ID(N'[dbo].[Faturamentos]'))
        EXEC sp_executesql N'CREATE INDEX [IX_Faturamentos_Cpf] ON [dbo].[Faturamentos]([Cpf]);';

    IF COL_LENGTH(N'[dbo].[Faturamentos]', N'Cnpj') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Faturamentos_Cnpj' AND object_id = OBJECT_ID(N'[dbo].[Faturamentos]'))
        EXEC sp_executesql N'CREATE INDEX [IX_Faturamentos_Cnpj] ON [dbo].[Faturamentos]([Cnpj]);';

    IF COL_LENGTH(N'[dbo].[Faturamentos]', N'Cpf') IS NOT NULL
       AND COL_LENGTH(N'[dbo].[Faturamentos]', N'Cnpj') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Faturamentos_Cpf_Cnpj_DataInicio_DataFim' AND object_id = OBJECT_ID(N'[dbo].[Faturamentos]'))
        EXEC sp_executesql N'CREATE INDEX [IX_Faturamentos_Cpf_Cnpj_DataInicio_DataFim] ON [dbo].[Faturamentos]([Cpf],[Cnpj],[DataInicio],[DataFim]);';
END
");

            // ------------------------------------------------------------
            // TRANSFERENCIAS / PEDIDOS / INBOUND WEBHOOK LOG (iguais aos seus, com guardas)
            // ------------------------------------------------------------
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Transferencias]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[Transferencias]', N'ExternalId') IS NULL
        ALTER TABLE [dbo].[Transferencias] ADD [ExternalId] NVARCHAR(80) NULL;
    IF COL_LENGTH(N'[dbo].[Transferencias]', N'GatewayTransactionId') IS NULL
        ALTER TABLE [dbo].[Transferencias] ADD [GatewayTransactionId] NVARCHAR(80) NULL;
    IF COL_LENGTH(N'[dbo].[Transferencias]', N'MetodoPagamento') IS NULL
        ALTER TABLE [dbo].[Transferencias] ADD [MetodoPagamento] INT NOT NULL CONSTRAINT DF_Transferencias_MetodoPagamento DEFAULT(0);

    DECLARE @df_t sysname, @sql_t NVARCHAR(MAX);
    SELECT @df_t = dc.name
      FROM sys.default_constraints dc
      JOIN sys.columns c ON c.default_object_id = dc.object_id
     WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Transferencias]')
       AND c.name = N'MetodoPagamento';
    IF @df_t IS NOT NULL
    BEGIN
        SET @sql_t = N'ALTER TABLE [dbo].[Transferencias] DROP CONSTRAINT ' + QUOTENAME(@df_t) + N';';
        EXEC sp_executesql @sql_t;
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Transferencias_ExternalId' AND object_id = OBJECT_ID(N'[dbo].[Transferencias]'))
        EXEC sp_executesql N'CREATE INDEX [IX_Transferencias_ExternalId] ON [dbo].[Transferencias]([ExternalId]);';
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Transferencias_GatewayTransactionId' AND object_id = OBJECT_ID(N'[dbo].[Transferencias]'))
        EXEC sp_executesql N'CREATE INDEX [IX_Transferencias_GatewayTransactionId] ON [dbo].[Transferencias]([GatewayTransactionId]);';
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Pedidos]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'[dbo].[Pedidos]', N'ExternalId') IS NULL
        ALTER TABLE [dbo].[Pedidos] ADD [ExternalId] NVARCHAR(80) NULL;
    IF COL_LENGTH(N'[dbo].[Pedidos]', N'GatewayTransactionId') IS NULL
        ALTER TABLE [dbo].[Pedidos] ADD [GatewayTransactionId] NVARCHAR(80) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pedidos_ExternalId' AND object_id = OBJECT_ID(N'[dbo].[Pedidos]'))
        EXEC sp_executesql N'CREATE INDEX [IX_Pedidos_ExternalId] ON [dbo].[Pedidos]([ExternalId]);';
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pedidos_GatewayTransactionId' AND object_id = OBJECT_ID(N'[dbo].[Pedidos]'))
        EXEC sp_executesql N'CREATE INDEX [IX_Pedidos_GatewayTransactionId] ON [dbo].[Pedidos]([GatewayTransactionId]);';
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[InboundWebhookLog]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InboundWebhookLog](
        [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_InboundWebhookLog] PRIMARY KEY,
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
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InboundWebhookLog_EventKey' AND object_id = OBJECT_ID(N'[dbo].[InboundWebhookLog]'))
    EXEC sp_executesql N'CREATE UNIQUE INDEX [IX_InboundWebhookLog_EventKey] ON [dbo].[InboundWebhookLog]([EventKey]);';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // (igual ao seu, já com guardas)

            // InboundWebhookLog
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[InboundWebhookLog]', N'U') IS NOT NULL
    DROP TABLE [dbo].[InboundWebhookLog];
");

            // Pedidos
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pedidos_GatewayTransactionId' AND object_id = OBJECT_ID(N'[dbo].[Pedidos]'))
    DROP INDEX [IX_Pedidos_GatewayTransactionId] ON [dbo].[Pedidos];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pedidos_ExternalId' AND object_id = OBJECT_ID(N'[dbo].[Pedidos]'))
    DROP INDEX [IX_Pedidos_ExternalId] ON [dbo].[Pedidos];

IF COL_LENGTH(N'[dbo].[Pedidos]', N'GatewayTransactionId') IS NOT NULL
    ALTER TABLE [dbo].[Pedidos] DROP COLUMN [GatewayTransactionId];
IF COL_LENGTH(N'[dbo].[Pedidos]', N'ExternalId') IS NOT NULL
    ALTER TABLE [dbo].[Pedidos] DROP COLUMN [ExternalId];
");

            // Transferencias
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Transferencias_GatewayTransactionId' AND object_id = OBJECT_ID(N'[dbo].[Transferencias]'))
    DROP INDEX [IX_Transferencias_GatewayTransactionId] ON [dbo].[Transferencias];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Transferencias_ExternalId' AND object_id = OBJECT_ID(N'[dbo].[Transferencias]'))
    DROP INDEX [IX_Transferencias_ExternalId] ON [dbo].[Transferencias];

IF COL_LENGTH(N'[dbo].[Transferencias]', N'MetodoPagamento') IS NOT NULL
    ALTER TABLE [dbo].[Transferencias] DROP COLUMN [MetodoPagamento];
IF COL_LENGTH(N'[dbo].[Transferencias]', N'GatewayTransactionId') IS NOT NULL
    ALTER TABLE [dbo].[Transferencias] DROP COLUMN [GatewayTransactionId];
IF COL_LENGTH(N'[dbo].[Transferencias]', N'ExternalId') IS NOT NULL
    ALTER TABLE [dbo].[Transferencias] DROP COLUMN [ExternalId];
");

            // Faturamentos
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Faturamentos_Cpf_Cnpj_DataInicio_DataFim' AND object_id = OBJECT_ID(N'[dbo].[Faturamentos]'))
    DROP INDEX [IX_Faturamentos_Cpf_Cnpj_DataInicio_DataFim] ON [dbo].[Faturamentos];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Faturamentos_Cnpj' AND object_id = OBJECT_ID(N'[dbo].[Faturamentos]'))
    DROP INDEX [IX_Faturamentos_Cnpj] ON [dbo].[Faturamentos];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Faturamentos_Cpf' AND object_id = OBJECT_ID(N'[dbo].[Faturamentos]'))
    DROP INDEX [IX_Faturamentos_Cpf] ON [dbo].[Faturamentos];

IF COL_LENGTH(N'[dbo].[Faturamentos]', N'Cnpj') IS NOT NULL
    ALTER TABLE [dbo].[Faturamentos] DROP COLUMN [Cnpj];
IF COL_LENGTH(N'[dbo].[Faturamentos]', N'Cpf') IS NOT NULL
    ALTER TABLE [dbo].[Faturamentos] DROP COLUMN [Cpf];

IF COL_LENGTH(N'[dbo].[Faturamentos]', N'CpfCnpj') IS NULL
    ALTER TABLE [dbo].[Faturamentos] ADD [CpfCnpj] NVARCHAR(14) NULL;

UPDATE F SET
    CpfCnpj = COALESCE(
        CASE WHEN LEN(F.Cnpj) = 14 THEN F.Cnpj END,
        CASE WHEN LEN(F.Cpf)  = 11 THEN F.Cpf  END,
        F.CpfCnpj
    )
FROM [dbo].[Faturamentos] F;
");

            // KycKybs
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'[dbo].[KycKybs]', N'Cnpj') IS NOT NULL
    ALTER TABLE [dbo].[KycKybs] DROP COLUMN [Cnpj];
IF COL_LENGTH(N'[dbo].[KycKybs]', N'Cpf') IS NOT NULL
    ALTER TABLE [dbo].[KycKybs] DROP COLUMN [Cpf];

IF COL_LENGTH(N'[dbo].[KycKybs]', N'CpfCnpj') IS NULL
    ALTER TABLE [dbo].[KycKybs] ADD [CpfCnpj] NVARCHAR(14) NULL;

UPDATE K SET
    CpfCnpj = COALESCE(
        CASE WHEN LEN(K.Cnpj) = 14 THEN K.Cnpj END,
        CASE WHEN LEN(K.Cpf)  = 11 THEN K.Cpf  END,
        K.CpfCnpj
    )
FROM [dbo].[KycKybs] K;

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_KycKyb_Cpf_Cnpj' AND parent_object_id = OBJECT_ID(N'[dbo].[KycKybs]'))
    ALTER TABLE [dbo].[KycKybs] DROP CONSTRAINT [CK_KycKyb_Cpf_Cnpj];
");

            // Usuarios
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Usuarios_Cpf_Cnpj_Tipo' AND parent_object_id = OBJECT_ID(N'[dbo].[Usuarios]'))
    ALTER TABLE [dbo].[Usuarios] DROP CONSTRAINT [CK_Usuarios_Cpf_Cnpj_Tipo];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Usuarios_Cnpj' AND object_id = OBJECT_ID(N'[dbo].[Usuarios]'))
    DROP INDEX [IX_Usuarios_Cnpj] ON [dbo].[Usuarios];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Usuarios_Cpf' AND object_id = OBJECT_ID(N'[dbo].[Usuarios]'))
    DROP INDEX [IX_Usuarios_Cpf] ON [dbo].[Usuarios];

IF COL_LENGTH(N'[dbo].[Usuarios]', N'CpfCnpj') IS NULL
    ALTER TABLE [dbo].[Usuarios] ADD [CpfCnpj] NVARCHAR(14) NULL;

UPDATE U SET
    CpfCnpj = COALESCE(
        CASE WHEN LEN(U.Cnpj) = 14 THEN U.Cnpj END,
        CASE WHEN LEN(U.Cpf)  = 11 THEN U.Cpf  END,
        U.CpfCnpj
    )
FROM [dbo].[Usuarios] U;

IF COL_LENGTH(N'[dbo].[Usuarios]', N'Cnpj') IS NOT NULL
    ALTER TABLE [dbo].[Usuarios] DROP COLUMN [Cnpj];
IF COL_LENGTH(N'[dbo].[Usuarios]', N'Cpf') IS NOT NULL
    ALTER TABLE [dbo].[Usuarios] DROP COLUMN [Cpf];
");
        }
    }
}
