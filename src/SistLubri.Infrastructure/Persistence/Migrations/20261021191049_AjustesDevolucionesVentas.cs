using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistLubri.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjustesDevolucionesVentas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MotivoAnulacion",
                table: "Ventas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DevueltaEnUtc",
                table: "Ventas",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DevueltaPorUsuarioId",
                table: "Ventas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoDevolucion",
                table: "Ventas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AnuladoPorUsuarioId",
                table: "ServiciosVehiculares",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreadoPorUsuarioId",
                table: "ServiciosVehiculares",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FinalizadoPorUsuarioId",
                table: "ServiciosVehiculares",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AfectaEfectivo",
                table: "PagosVenta",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EsAnulacion",
                table: "PagosVenta",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EsReintegro",
                table: "PagosVenta",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "MovimientoCajaId",
                table: "PagosVenta",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoOperacion",
                table: "PagosVenta",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioId",
                table: "MovimientosInventario",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PagoVentaId",
                table: "MovimientosCaja",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumeroCierres",
                table: "JornadasCaja",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "CantidadDevuelta",
                table: "DetallesVenta",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "UnidadMedida",
                table: "DetallesServicio",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "DuracionJornada",
                table: "ConfiguracionCaja",
                type: "time",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RegistrosAperturaCaja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JornadaCajaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    FechaUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaldoInicial = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosAperturaCaja", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosAperturaCaja_JornadasCaja_JornadaCajaId",
                        column: x => x.JornadaCajaId,
                        principalTable: "JornadasCaja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosCierreCaja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JornadaCajaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CerradoEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CerradoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EfectivoEsperado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EfectivoContado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Diferencia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VerificadoEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    VerificadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReabiertoEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReabiertoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MotivoReapertura = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProximoCierreId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosCierreCaja", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosCierreCaja_JornadasCaja_JornadaCajaId",
                        column: x => x.JornadaCajaId,
                        principalTable: "JornadasCaja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReintegrosVenta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VentaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedioPagoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreMedioPago = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Importe = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AfectaEfectivo = table.Column<bool>(type: "bit", nullable: false),
                    EsAnulacion = table.Column<bool>(type: "bit", nullable: false),
                    MovimientoCajaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReintegrosVenta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReintegrosVenta_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PagosVenta_MovimientoCajaId",
                table: "PagosVenta",
                column: "MovimientoCajaId",
                unique: true,
                filter: "[MovimientoCajaId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_UsuarioId",
                table: "MovimientosInventario",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCaja_PagoVentaId",
                table: "MovimientosCaja",
                column: "PagoVentaId",
                unique: true,
                filter: "[PagoVentaId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAperturaCaja_JornadaCajaId_Numero",
                table: "RegistrosAperturaCaja",
                columns: new[] { "JornadaCajaId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosCierreCaja_JornadaCajaId_Numero",
                table: "RegistrosCierreCaja",
                columns: new[] { "JornadaCajaId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosCierreCaja_ProximoCierreId",
                table: "RegistrosCierreCaja",
                column: "ProximoCierreId",
                unique: true,
                filter: "[ProximoCierreId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReintegrosVenta_MedioPagoId",
                table: "ReintegrosVenta",
                column: "MedioPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_ReintegrosVenta_MovimientoCajaId",
                table: "ReintegrosVenta",
                column: "MovimientoCajaId",
                unique: true,
                filter: "[MovimientoCajaId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReintegrosVenta_UsuarioId",
                table: "ReintegrosVenta",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ReintegrosVenta_VentaId",
                table: "ReintegrosVenta",
                column: "VentaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosAperturaCaja");

            migrationBuilder.DropTable(
                name: "RegistrosCierreCaja");

            migrationBuilder.DropTable(
                name: "ReintegrosVenta");

            migrationBuilder.DropIndex(
                name: "IX_PagosVenta_MovimientoCajaId",
                table: "PagosVenta");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventario_UsuarioId",
                table: "MovimientosInventario");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosCaja_PagoVentaId",
                table: "MovimientosCaja");

            migrationBuilder.DropColumn(
                name: "DevueltaEnUtc",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "DevueltaPorUsuarioId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MotivoDevolucion",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "AnuladoPorUsuarioId",
                table: "ServiciosVehiculares");

            migrationBuilder.DropColumn(
                name: "CreadoPorUsuarioId",
                table: "ServiciosVehiculares");

            migrationBuilder.DropColumn(
                name: "FinalizadoPorUsuarioId",
                table: "ServiciosVehiculares");

            migrationBuilder.DropColumn(
                name: "AfectaEfectivo",
                table: "PagosVenta");

            migrationBuilder.DropColumn(
                name: "EsAnulacion",
                table: "PagosVenta");

            migrationBuilder.DropColumn(
                name: "EsReintegro",
                table: "PagosVenta");

            migrationBuilder.DropColumn(
                name: "MovimientoCajaId",
                table: "PagosVenta");

            migrationBuilder.DropColumn(
                name: "TipoOperacion",
                table: "PagosVenta");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "MovimientosInventario");

            migrationBuilder.DropColumn(
                name: "PagoVentaId",
                table: "MovimientosCaja");

            migrationBuilder.DropColumn(
                name: "NumeroCierres",
                table: "JornadasCaja");

            migrationBuilder.DropColumn(
                name: "CantidadDevuelta",
                table: "DetallesVenta");

            migrationBuilder.DropColumn(
                name: "DuracionJornada",
                table: "ConfiguracionCaja");

            migrationBuilder.AlterColumn<string>(
                name: "MotivoAnulacion",
                table: "Ventas",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UnidadMedida",
                table: "DetallesServicio",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);
        }
    }
}
