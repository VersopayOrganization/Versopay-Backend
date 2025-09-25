using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VersopayDatabase.Migrations
{
    /// <inheritdoc />
    public partial class AddPropCadastrCompletoInTabelUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CadastroCompleto",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.CreateIndex(
                name: "IX_Faturamentos_CpfCnpj",
                table: "Faturamentos",
                column: "CpfCnpj");

            migrationBuilder.CreateIndex(
                name: "IX_Faturamentos_CpfCnpj_DataInicio_DataFim",
                table: "Faturamentos",
                columns: new[] { "CpfCnpj", "DataInicio", "DataFim" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Faturamentos");

            migrationBuilder.DropColumn(
                name: "CadastroCompleto",
                table: "Usuarios");
        }
    }
}
