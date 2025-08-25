using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VersopayDatabase.Migrations
{
    /// <inheritdoc />
    public partial class Usuarios_CpfCnpj_Nullableparte2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Derruba índice antigo (sem filtro), se existir
            try
            {
                migrationBuilder.DropIndex(
                    name: "IX_Usuarios_CpfCnpj",
                    table: "Usuarios");
            }
            catch { }

            // Derruba constraint antiga, se existir
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Usuarios_CpfCnpj_Tipo')
                    ALTER TABLE [Usuarios] DROP CONSTRAINT [CK_Usuarios_CpfCnpj_Tipo];
            ");

            // Torna TipoCadastro NULLABLE
            migrationBuilder.AlterColumn<int>(
                name: "TipoCadastro",
                table: "Usuarios",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            // Torna CpfCnpj NULLABLE
            migrationBuilder.AlterColumn<string>(
                name: "CpfCnpj",
                table: "Usuarios",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(14)",
                oldMaxLength: 14);

            // Recria índice único com filtro
            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CpfCnpj",
                table: "Usuarios",
                column: "CpfCnpj",
                unique: true,
                filter: "[CpfCnpj] IS NOT NULL");

            // Recria a constraint permitindo nulos
            migrationBuilder.Sql(@"
                ALTER TABLE [Usuarios] ADD CONSTRAINT [CK_Usuarios_CpfCnpj_Tipo]
                CHECK ((TipoCadastro IS NULL AND [CpfCnpj] IS NULL)
                    OR (TipoCadastro = 0 AND LEN([CpfCnpj]) = 11)
                    OR (TipoCadastro = 1 AND LEN([CpfCnpj]) = 14));
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_CpfCnpj",
                table: "Usuarios");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Usuarios_CpfCnpj_Tipo')
                    ALTER TABLE [Usuarios] DROP CONSTRAINT [CK_Usuarios_CpfCnpj_Tipo];
            ");

            migrationBuilder.AlterColumn<int>(
                name: "TipoCadastro",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

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

            migrationBuilder.Sql(@"
                ALTER TABLE [Usuarios] ADD CONSTRAINT [CK_Usuarios_CpfCnpj_Tipo]
                CHECK ((TipoCadastro = 0 AND LEN([CpfCnpj]) = 11)
                    OR (TipoCadastro = 1 AND LEN([CpfCnpj]) = 14));
            ");
        }
    }
}
