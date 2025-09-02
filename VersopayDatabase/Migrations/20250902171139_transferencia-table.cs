using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VersopayDatabase.Migrations
{
    /// <inheritdoc />
    public partial class transferenciatable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Transferencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SolicitanteId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DataSolicitacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValorSolicitado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Empresa = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    ChavePix = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Aprovacao = table.Column<int>(type: "int", nullable: false),
                    TipoEnvio = table.Column<int>(type: "int", nullable: true),
                    Taxa = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ValorFinal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DataCadastro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAprovacao = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transferencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transferencias_Usuarios_SolicitanteId",
                        column: x => x.SolicitanteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_SolicitanteId",
                table: "Transferencias",
                column: "SolicitanteId");

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_Status_DataSolicitacao",
                table: "Transferencias",
                columns: new[] { "Status", "DataSolicitacao" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transferencias");
        }
    }
}
