using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VersopayDatabase.Migrations
{
    /// <inheritdoc />
    public partial class VexyBankWebhookChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiKey",
                table: "ProviderCredentials",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApiSecret",
                table: "ProviderCredentials",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebhookSignatureSecret",
                table: "ProviderCredentials",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiKey",
                table: "ProviderCredentials");

            migrationBuilder.DropColumn(
                name: "ApiSecret",
                table: "ProviderCredentials");

            migrationBuilder.DropColumn(
                name: "WebhookSignatureSecret",
                table: "ProviderCredentials");
        }
    }
}
