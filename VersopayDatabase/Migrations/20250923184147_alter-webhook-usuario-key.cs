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
            migrationBuilder.AddColumn<int>(
                name: "OwnerUserId",
                table: "Webhooks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Transferencias",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GatewayTransactionId",
                table: "Transferencias",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Pedidos",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GatewayTransactionId",
                table: "Pedidos",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Faturamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CpfCnpj = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    DataInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataFim = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VendasTotais = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VendasCartao = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VendasBoleto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VendasPix = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reserva = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VendasCanceladas = table.Column<int>(type: "int", nullable: false),
                    DiasSemVendas = table.Column<int>(type: "int", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faturamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboundWebhookLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Provedor = table.Column<int>(type: "int", nullable: false),
                    Evento = table.Column<int>(type: "int", nullable: false),
                    EventKey = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    TransactionId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ExternalId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    RequestNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    TipoTransacao = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Fee = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DebtorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DebtorDocument = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ispb = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NomeRecebedor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CpfRecebedor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataEventoUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SourceIp = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HeadersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessingStatus = table.Column<int>(type: "int", nullable: false),
                    ProcessingError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PedidoId = table.Column<int>(type: "int", nullable: true),
                    TransferenciaId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundWebhookLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProviderCredentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerUserId = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ClientSecret = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    AccessTokenExpiresUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CriadoEmUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderCredentials_Usuarios_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Webhooks_OwnerUserId_Ativo",
                table: "Webhooks",
                columns: new[] { "OwnerUserId", "Ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_ExternalId",
                table: "Transferencias",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_GatewayTransactionId",
                table: "Transferencias",
                column: "GatewayTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_ExternalId",
                table: "Pedidos",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_GatewayTransactionId",
                table: "Pedidos",
                column: "GatewayTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Faturamentos_CpfCnpj",
                table: "Faturamentos",
                column: "CpfCnpj");

            migrationBuilder.CreateIndex(
                name: "IX_Faturamentos_CpfCnpj_DataInicio_DataFim",
                table: "Faturamentos",
                columns: new[] { "CpfCnpj", "DataInicio", "DataFim" });

            migrationBuilder.CreateIndex(
                name: "IX_InboundWebhookLog_EventKey",
                table: "InboundWebhookLog",
                column: "EventKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderCredentials_OwnerUserId_Provider",
                table: "ProviderCredentials",
                columns: new[] { "OwnerUserId", "Provider" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Webhooks_Usuarios_OwnerUserId",
                table: "Webhooks",
                column: "OwnerUserId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
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
