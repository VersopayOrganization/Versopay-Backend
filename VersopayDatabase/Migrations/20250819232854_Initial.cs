using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VersopayDatabase.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    SenhaHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoCadastro = table.Column<int>(type: "int", nullable: false),
                    CpfCnpj = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                    Instagram = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Telefone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.CheckConstraint("CK_Usuarios_CpfCnpj_Tipo", "( (TipoCadastro = 0 AND LEN([CpfCnpj]) = 11) OR (TipoCadastro = 1 AND LEN([CpfCnpj]) = 14) )");
                });

            migrationBuilder.CreateTable(
                name: "Documentos",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    FrenteRgCaminho = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    VersoRgCaminho = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    SelfieDocCaminho = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    CartaoCnpjCaminho = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    FrenteRgStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    VersoRgStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    SelfieDocStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CartaoCnpjStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    FrenteRgAssinaturaSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    VersoRgAssinaturaSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SelfieDocAssinaturaSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CartaoCnpjAssinaturaSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DataAtualizacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documentos", x => x.UsuarioId);
                    table.ForeignKey(
                        name: "FK_Documentos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiraEmUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevogadoEmUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubstituidoPorHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Ip = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Dispositivo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UsuarioId",
                table: "RefreshTokens",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CpfCnpj",
                table: "Usuarios",
                column: "CpfCnpj",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documentos");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
