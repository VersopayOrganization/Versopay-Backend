using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VersopayDatabase.Migrations
{
    /// <inheritdoc />
    public partial class alterwebhookusuariokey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // WEBHOOKS.OwnerUserId
            migrationBuilder.Sql(@"
            IF COL_LENGTH('dbo.Webhooks','OwnerUserId') IS NULL
                ALTER TABLE dbo.Webhooks ADD OwnerUserId int NOT NULL CONSTRAINT DF_Webhooks_OwnerUserId DEFAULT(0);
            ");

            // TRANSFERENCIAS.ExternalId
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Transferencias','ExternalId') IS NULL
    ALTER TABLE dbo.Transferencias ADD ExternalId nvarchar(80) NULL;
");

            // TRANSFERENCIAS.GatewayTransactionId
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Transferencias','GatewayTransactionId') IS NULL
    ALTER TABLE dbo.Transferencias ADD GatewayTransactionId nvarchar(80) NULL;
");

            // PEDIDOS.ExternalId
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Pedidos','ExternalId') IS NULL
    ALTER TABLE dbo.Pedidos ADD ExternalId nvarchar(80) NULL;
");

            // PEDIDOS.GatewayTransactionId
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Pedidos','GatewayTransactionId') IS NULL
    ALTER TABLE dbo.Pedidos ADD GatewayTransactionId nvarchar(80) NULL;
");

            // Índices (só cria se não existir)
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Transferencias_ExternalId' AND object_id=OBJECT_ID('dbo.Transferencias'))
    CREATE INDEX IX_Transferencias_ExternalId ON dbo.Transferencias(ExternalId);
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Transferencias_GatewayTransactionId' AND object_id=OBJECT_ID('dbo.Transferencias'))
    CREATE INDEX IX_Transferencias_GatewayTransactionId ON dbo.Transferencias(GatewayTransactionId);
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Pedidos_ExternalId' AND object_id=OBJECT_ID('dbo.Pedidos'))
    CREATE INDEX IX_Pedidos_ExternalId ON dbo.Pedidos(ExternalId);
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Pedidos_GatewayTransactionId' AND object_id=OBJECT_ID('dbo.Pedidos'))
    CREATE INDEX IX_Pedidos_GatewayTransactionId ON dbo.Pedidos(GatewayTransactionId);
");

            // Tabelas (só cria se não existir)
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.Faturamentos','U') IS NULL
BEGIN
    CREATE TABLE dbo.Faturamentos(
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Faturamentos PRIMARY KEY,
        CpfCnpj nvarchar(14) NOT NULL,
        DataInicio datetime2 NOT NULL,
        DataFim datetime2 NOT NULL,
        VendasTotais decimal(18,2) NOT NULL,
        VendasCartao decimal(18,2) NOT NULL,
        VendasBoleto decimal(18,2) NOT NULL,
        VendasPix decimal(18,2) NOT NULL,
        Reserva decimal(18,2) NOT NULL,
        VendasCanceladas int NOT NULL,
        DiasSemVendas int NOT NULL,
        AtualizadoEmUtc datetime2 NOT NULL
    );
    CREATE INDEX IX_Faturamentos_CpfCnpj ON dbo.Faturamentos(CpfCnpj);
    CREATE INDEX IX_Faturamentos_CpfCnpj_DataInicio_DataFim ON dbo.Faturamentos(CpfCnpj, DataInicio, DataFim);
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.InboundWebhookLog','U') IS NULL
BEGIN
    CREATE TABLE dbo.InboundWebhookLog(
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_InboundWebhookLog PRIMARY KEY,
        Provedor int NOT NULL,
        Evento int NOT NULL,
        EventKey nvarchar(180) NOT NULL,
        TransactionId nvarchar(80) NULL,
        ExternalId nvarchar(80) NULL,
        RequestNumber nvarchar(80) NULL,
        Status nvarchar(40) NULL,
        TipoTransacao nvarchar(20) NULL,
        Valor decimal(18,2) NULL,
        Fee decimal(18,2) NULL,
        NetAmount decimal(18,2) NULL,
        DebtorName nvarchar(max) NULL,
        DebtorDocument nvarchar(max) NULL,
        Ispb nvarchar(max) NULL,
        NomeRecebedor nvarchar(max) NULL,
        CpfRecebedor nvarchar(max) NULL,
        DataEventoUtc datetime2 NULL,
        SourceIp nvarchar(max) NOT NULL,
        HeadersJson nvarchar(max) NOT NULL,
        PayloadJson nvarchar(max) NOT NULL,
        ReceivedAtUtc datetime2 NOT NULL,
        ProcessedAtUtc datetime2 NULL,
        ProcessingStatus int NOT NULL,
        ProcessingError nvarchar(max) NULL,
        PedidoId int NULL,
        TransferenciaId int NULL
    );
    CREATE UNIQUE INDEX IX_InboundWebhookLog_EventKey ON dbo.InboundWebhookLog(EventKey);
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.ProviderCredentials','U') IS NULL
BEGIN
    CREATE TABLE dbo.ProviderCredentials(
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProviderCredentials PRIMARY KEY,
        OwnerUserId int NOT NULL,
        Provider int NOT NULL,
        ClientId nvarchar(120) NOT NULL,
        ClientSecret nvarchar(160) NOT NULL,
        AccessToken nvarchar(600) NULL,
        AccessTokenExpiresUtc datetime2 NULL,
        CriadoEmUtc datetime2 NOT NULL,
        AtualizadoEmUtc datetime2 NULL
    );
    CREATE UNIQUE INDEX IX_ProviderCredentials_OwnerUserId_Provider ON dbo.ProviderCredentials(OwnerUserId, Provider);
END
");

            // FK (só cria se não existir)
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_Webhooks_Usuarios_OwnerUserId')
BEGIN
    ALTER TABLE dbo.Webhooks WITH CHECK
    ADD CONSTRAINT FK_Webhooks_Usuarios_OwnerUserId FOREIGN KEY(OwnerUserId) REFERENCES dbo.Usuarios(Id);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Webhooks_Usuarios_OwnerUserId",
                table: "Webhooks");

            migrationBuilder.DropTable(
                name: "Faturamentos");

            migrationBuilder.DropTable(
                name: "InboundWebhookLog");

            migrationBuilder.DropTable(
                name: "ProviderCredentials");

            migrationBuilder.DropIndex(
                name: "IX_Webhooks_OwnerUserId_Ativo",
                table: "Webhooks");

            migrationBuilder.DropIndex(
                name: "IX_Transferencias_ExternalId",
                table: "Transferencias");

            migrationBuilder.DropIndex(
                name: "IX_Transferencias_GatewayTransactionId",
                table: "Transferencias");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_ExternalId",
                table: "Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_GatewayTransactionId",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Webhooks");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Transferencias");

            migrationBuilder.DropColumn(
                name: "GatewayTransactionId",
                table: "Transferencias");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "GatewayTransactionId",
                table: "Pedidos");
        }
    }
}
