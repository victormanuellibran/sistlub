using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistLubri.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Gastos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GastoId",
                table: "MovimientosCaja",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PagoGastoId",
                table: "MovimientosCaja",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReintegroGastoId",
                table: "MovimientosCaja",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ActualizadaEnUtc",
                table: "JornadasCaja",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "JornadasCaja",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.CreateTable(
                name: "Gastos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CategoriaDetalle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    ImporteTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EntidadRelacionada = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProveedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaVencimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    VencimientoEstimado = table.Column<bool>(type: "bit", nullable: false),
                    ReferenciaComprobante = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreadoEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActualizadoEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnuladoEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AnuladoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gastos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Gastos_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PagosGasto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GastoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedioPagoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Importe = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreadoEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosGasto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagosGasto_Gastos_GastoId",
                        column: x => x.GastoId,
                        principalTable: "Gastos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReintegrosGasto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GastoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedioPagoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Importe = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreadoEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReintegrosGasto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReintegrosGasto_Gastos_GastoId",
                        column: x => x.GastoId,
                        principalTable: "Gastos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCaja_GastoId",
                table: "MovimientosCaja",
                column: "GastoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCaja_PagoGastoId",
                table: "MovimientosCaja",
                column: "PagoGastoId",
                unique: true,
                filter: "[PagoGastoId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCaja_ReintegroGastoId",
                table: "MovimientosCaja",
                column: "ReintegroGastoId",
                unique: true,
                filter: "[ReintegroGastoId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Gastos_Categoria",
                table: "Gastos",
                column: "Categoria");

            migrationBuilder.CreateIndex(
                name: "IX_Gastos_CreadoPorUsuarioId",
                table: "Gastos",
                column: "CreadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Gastos_Estado",
                table: "Gastos",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Gastos_Fecha",
                table: "Gastos",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Gastos_FechaVencimiento",
                table: "Gastos",
                column: "FechaVencimiento");

            migrationBuilder.CreateIndex(
                name: "IX_Gastos_ProveedorId",
                table: "Gastos",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_PagosGasto_GastoId_Fecha",
                table: "PagosGasto",
                columns: new[] { "GastoId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_ReintegrosGasto_GastoId_Fecha",
                table: "ReintegrosGasto",
                columns: new[] { "GastoId", "Fecha" });

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosCaja_Gastos_GastoId",
                table: "MovimientosCaja",
                column: "GastoId",
                principalTable: "Gastos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosCaja_PagosGasto_PagoGastoId",
                table: "MovimientosCaja",
                column: "PagoGastoId",
                principalTable: "PagosGasto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosCaja_ReintegrosGasto_ReintegroGastoId",
                table: "MovimientosCaja",
                column: "ReintegroGastoId",
                principalTable: "ReintegrosGasto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosCaja_Gastos_GastoId",
                table: "MovimientosCaja");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosCaja_PagosGasto_PagoGastoId",
                table: "MovimientosCaja");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosCaja_ReintegrosGasto_ReintegroGastoId",
                table: "MovimientosCaja");

            migrationBuilder.DropTable(
                name: "PagosGasto");

            migrationBuilder.DropTable(
                name: "ReintegrosGasto");

            migrationBuilder.DropTable(
                name: "Gastos");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosCaja_GastoId",
                table: "MovimientosCaja");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosCaja_PagoGastoId",
                table: "MovimientosCaja");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosCaja_ReintegroGastoId",
                table: "MovimientosCaja");

            migrationBuilder.DropColumn(
                name: "GastoId",
                table: "MovimientosCaja");

            migrationBuilder.DropColumn(
                name: "PagoGastoId",
                table: "MovimientosCaja");

            migrationBuilder.DropColumn(
                name: "ReintegroGastoId",
                table: "MovimientosCaja");

            migrationBuilder.DropColumn(
                name: "ActualizadaEnUtc",
                table: "JornadasCaja");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "JornadasCaja");
        }
    }
}
