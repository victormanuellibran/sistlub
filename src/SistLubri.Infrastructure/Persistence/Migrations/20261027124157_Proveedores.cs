using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistLubri.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Proveedores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RazonSocial = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Cuit = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    ContactoNombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    MarcasYProductos = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CondicionPagoHabitual = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PlazoPagoHabitualDias = table.Column<int>(type: "int", nullable: true),
                    PeriodoPagoHabitual = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CreadoEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActualizadoEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosEstadoProveedor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProveedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosEstadoProveedor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosEstadoProveedor_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_Activo",
                table: "Proveedores",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_Cuit",
                table: "Proveedores",
                column: "Cuit",
                unique: true,
                filter: "[Cuit] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_RazonSocial",
                table: "Proveedores",
                column: "RazonSocial");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_Telefono",
                table: "Proveedores",
                column: "Telefono");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosEstadoProveedor_ProveedorId_FechaUtc",
                table: "RegistrosEstadoProveedor",
                columns: new[] { "ProveedorId", "FechaUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosEstadoProveedor");

            migrationBuilder.DropTable(
                name: "Proveedores");
        }
    }
}
