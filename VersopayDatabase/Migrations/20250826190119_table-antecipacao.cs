using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VersopayDatabase.Migrations
{
    /// <inheritdoc />
    public partial class tableantecipacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Antecipacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DataSolicitacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Antecipacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Antecipacoes_Usuarios_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Antecipacoes_EmpresaId",
                table: "Antecipacoes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Antecipacoes_Status",
                table: "Antecipacoes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Antecipacoes_Status_DataSolicitacao",
                table: "Antecipacoes",
                columns: new[] { "Status", "DataSolicitacao" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Antecipacoes");
        }
    }
}
