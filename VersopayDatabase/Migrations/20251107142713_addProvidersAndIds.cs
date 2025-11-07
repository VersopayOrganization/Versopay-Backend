using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VersopayDatabase.Migrations
{
    /// <inheritdoc />
    public partial class addProvidersAndIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PedidoId",
                table: "VexyBankPixIns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Provider",
                table: "Transferencias",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Provider",
                table: "Pedidos",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PedidoId",
                table: "VexyBankPixIns");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "Transferencias");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "Pedidos");
        }
    }
}
