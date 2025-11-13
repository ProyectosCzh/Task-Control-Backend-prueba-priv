using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace taskcontrolv1.Migrations
{
    /// <inheritdoc />
    public partial class migration3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Departamento",
                table: "Usuarios",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NivelHabilidad",
                table: "Usuarios",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Capacidades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capacidades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Capacidades_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioCapacidades",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CapacidadId = table.Column<int>(type: "int", nullable: false),
                    Nivel = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioCapacidades", x => new { x.UsuarioId, x.CapacidadId });
                    table.ForeignKey(
                        name: "FK_UsuarioCapacidades_Capacidades_CapacidadId",
                        column: x => x.CapacidadId,
                        principalTable: "Capacidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioCapacidades_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Capacidades_EmpresaId_Nombre",
                table: "Capacidades",
                columns: new[] { "EmpresaId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioCapacidades_CapacidadId",
                table: "UsuarioCapacidades",
                column: "CapacidadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsuarioCapacidades");

            migrationBuilder.DropTable(
                name: "Capacidades");

            migrationBuilder.DropColumn(
                name: "Departamento",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "NivelHabilidad",
                table: "Usuarios");
        }
    }
}
