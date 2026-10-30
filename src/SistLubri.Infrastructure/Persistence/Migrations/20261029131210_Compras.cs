using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistLubri.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Compras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FacturaCompraId",
                table: "MovimientosCaja",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PagoCompraId",
                table: "MovimientosCaja",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Compras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProveedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreadoEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActualizadoEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CerradaEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CerradaPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MotivoCierre = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AnuladaEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AnuladaPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Compras_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesCompra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoProducto = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    NombreProducto = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    UnidadMedida = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CantidadSolicitada = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    CantidadAceptada = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    CostoUnitarioEstimado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalRecepcionDanada = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesCompra_Compras_CompraId",
                        column: x => x.CompraId,
                        principalTable: "Compras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesCompra_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FacturasCompra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaVencimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    ImporteTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreadaEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturasCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacturasCompra_Compras_CompraId",
                        column: x => x.CompraId,
                        principalTable: "Compras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecepcionesCompra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Remito = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreadaEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecepcionesCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecepcionesCompra_Compras_CompraId",
                        column: x => x.CompraId,
                        principalTable: "Compras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PagosCompra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacturaCompraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    MedioPagoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Importe = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreadoEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagosCompra_FacturasCompra_FacturaCompraId",
                        column: x => x.FacturaCompraId,
                        principalTable: "FacturasCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PagosCompra_MediosPago_MedioPagoId",
                        column: x => x.MedioPagoId,
                        principalTable: "MediosPago",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LineasRecepcionCompra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecepcionCompraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DetalleCompraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CantidadAceptada = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    CantidadDanada = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineasRecepcionCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineasRecepcionCompra_DetallesCompra_DetalleCompraId",
                        column: x => x.DetalleCompraId,
                        principalTable: "DetallesCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LineasRecepcionCompra_RecepcionesCompra_RecepcionCompraId",
                        column: x => x.RecepcionCompraId,
                        principalTable: "RecepcionesCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCaja_FacturaCompraId",
                table: "MovimientosCaja",
                column: "FacturaCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCaja_PagoCompraId",
                table: "MovimientosCaja",
                column: "PagoCompraId",
                unique: true,
                filter: "[PagoCompraId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_Fecha",
                table: "Compras",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Compras_ProveedorId",
                table: "Compras",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesCompra_CompraId_ProductoId",
                table: "DetallesCompra",
                columns: new[] { "CompraId", "ProductoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetallesCompra_ProductoId",
                table: "DetallesCompra",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturasCompra_CompraId_Numero",
                table: "FacturasCompra",
                columns: new[] { "CompraId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LineasRecepcionCompra_DetalleCompraId",
                table: "LineasRecepcionCompra",
                column: "DetalleCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_LineasRecepcionCompra_RecepcionCompraId_DetalleCompraId",
                table: "LineasRecepcionCompra",
                columns: new[] { "RecepcionCompraId", "DetalleCompraId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PagosCompra_FacturaCompraId_Fecha",
                table: "PagosCompra",
                columns: new[] { "FacturaCompraId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_PagosCompra_MedioPagoId",
                table: "PagosCompra",
                column: "MedioPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_RecepcionesCompra_CompraId_Fecha",
                table: "RecepcionesCompra",
                columns: new[] { "CompraId", "Fecha" });

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosCaja_FacturasCompra_FacturaCompraId",
                table: "MovimientosCaja",
                column: "FacturaCompraId",
                principalTable: "FacturasCompra",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosCaja_PagosCompra_PagoCompraId",
                table: "MovimientosCaja",
                column: "PagoCompraId",
                principalTable: "PagosCompra",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosCaja_FacturasCompra_FacturaCompraId",
                table: "MovimientosCaja");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosCaja_PagosCompra_PagoCompraId",
                table: "MovimientosCaja");

            migrationBuilder.DropTable(
                name: "LineasRecepcionCompra");

            migrationBuilder.DropTable(
                name: "PagosCompra");

            migrationBuilder.DropTable(
                name: "DetallesCompra");

            migrationBuilder.DropTable(
                name: "RecepcionesCompra");

            migrationBuilder.DropTable(
                name: "FacturasCompra");

            migrationBuilder.DropTable(
                name: "Compras");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosCaja_FacturaCompraId",
                table: "MovimientosCaja");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosCaja_PagoCompraId",
                table: "MovimientosCaja");

            migrationBuilder.DropColumn(
                name: "FacturaCompraId",
                table: "MovimientosCaja");

            migrationBuilder.DropColumn(
                name: "PagoCompraId",
                table: "MovimientosCaja");
        }
    }
}
