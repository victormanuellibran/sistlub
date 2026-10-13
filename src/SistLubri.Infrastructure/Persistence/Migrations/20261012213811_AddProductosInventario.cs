using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistLubri.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddProductosInventario : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Productos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Codigo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Marca = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                Categoria = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                Presentacion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                UnidadMedida = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                PrecioVenta = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                CostoCompra = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                StockActual = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                StockMinimo = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                EsFraccionado = table.Column<bool>(type: "bit", nullable: false),
                Activo = table.Column<bool>(type: "bit", nullable: false),
                CreadoEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ActualizadoEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Productos", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "MovimientosInventario",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                Cantidad = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                StockAnterior = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                StockResultante = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                ReferenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                FechaUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MovimientosInventario", x => x.Id);
                table.ForeignKey(
                    name: "FK_MovimientosInventario_Productos_ProductoId",
                    column: x => x.ProductoId,
                    principalTable: "Productos",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MovimientosInventario_FechaUtc",
            table: "MovimientosInventario",
            column: "FechaUtc");

        migrationBuilder.CreateIndex(
            name: "IX_MovimientosInventario_ProductoId",
            table: "MovimientosInventario",
            column: "ProductoId");

        migrationBuilder.CreateIndex(
            name: "IX_MovimientosInventario_ReferenciaId",
            table: "MovimientosInventario",
            column: "ReferenciaId");

        migrationBuilder.CreateIndex(
            name: "IX_MovimientosInventario_Tipo",
            table: "MovimientosInventario",
            column: "Tipo");

        migrationBuilder.CreateIndex(
            name: "IX_Productos_Categoria",
            table: "Productos",
            column: "Categoria");

        migrationBuilder.CreateIndex(
            name: "IX_Productos_Codigo",
            table: "Productos",
            column: "Codigo",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Productos_Marca",
            table: "Productos",
            column: "Marca");

        migrationBuilder.CreateIndex(
            name: "IX_Productos_Nombre",
            table: "Productos",
            column: "Nombre");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MovimientosInventario");

        migrationBuilder.DropTable(
            name: "Productos");
    }
}
