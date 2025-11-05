using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VersopayDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddPrevWebhookSecret_to_ProviderCredential : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.RenameTable(
                name: "ProviderCredentials",
                newName: "ProviderCredentials",
                newSchema: "dbo");

            migrationBuilder.AlterColumn<string>(
                name: "ClientSecret",
                schema: "dbo",
                table: "ProviderCredentials",
                type: "nvarchar(600)",
                maxLength: 600,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "ClientId",
                schema: "dbo",
                table: "ProviderCredentials",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "PrevWebhookSignatureSecret",
                schema: "dbo",
                table: "ProviderCredentials",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrevWebhookSignatureSecret",
                schema: "dbo",
                table: "ProviderCredentials");

            migrationBuilder.RenameTable(
                name: "ProviderCredentials",
                schema: "dbo",
                newName: "ProviderCredentials");

            migrationBuilder.AlterColumn<string>(
                name: "ClientSecret",
                table: "ProviderCredentials",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(600)",
                oldMaxLength: 600);

            migrationBuilder.AlterColumn<string>(
                name: "ClientId",
                table: "ProviderCredentials",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512);
        }
    }
}
