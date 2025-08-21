using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VersopayDatabase.Migrations
{
    /// <inheritdoc />
    public partial class Rename_ResetToken_Columns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UsadoEmUtc",
                table: "NovaSenhaResetTokens",
                newName: "DataTokenUsado");

            migrationBuilder.RenameColumn(
                name: "ExpiraEmUtc",
                table: "NovaSenhaResetTokens",
                newName: "DataSolicitacao");

            migrationBuilder.RenameColumn(
                name: "CriadoEmUtc",
                table: "NovaSenhaResetTokens",
                newName: "DataExpiracao");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DataTokenUsado",
                table: "NovaSenhaResetTokens",
                newName: "UsadoEmUtc");

            migrationBuilder.RenameColumn(
                name: "DataSolicitacao",
                table: "NovaSenhaResetTokens",
                newName: "ExpiraEmUtc");

            migrationBuilder.RenameColumn(
                name: "DataExpiracao",
                table: "NovaSenhaResetTokens",
                newName: "CriadoEmUtc");
        }
    }
}
