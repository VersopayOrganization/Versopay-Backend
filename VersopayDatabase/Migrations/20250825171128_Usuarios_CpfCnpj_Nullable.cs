using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VersopayDatabase.Migrations
{
    /// <inheritdoc />
    public partial class Usuarios_CpfCnpj_Nullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_CpfCnpj",
                table: "Usuarios");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Usuarios_CpfCnpj_Tipo",
                table: "Usuarios");

            migrationBuilder.AlterColumn<string>(
                name: "CpfCnpj",
                table: "Usuarios",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(14)",
                oldMaxLength: 14);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_CpfCnpj",
                table: "Usuarios");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Usuarios_CpfCnpj_Tipo",
                table: "Usuarios");

            migrationBuilder.AlterColumn<string>(
                name: "CpfCnpj",
                table: "Usuarios",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(14)",
                oldMaxLength: 14,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CpfCnpj",
                table: "Usuarios",
                column: "CpfCnpj",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Usuarios_CpfCnpj_Tipo",
                table: "Usuarios",
                sql: "( (TipoCadastro = 0 AND LEN([CpfCnpj]) = 11) OR (TipoCadastro = 1 AND LEN([CpfCnpj]) = 14) )");
        }
    }
}
