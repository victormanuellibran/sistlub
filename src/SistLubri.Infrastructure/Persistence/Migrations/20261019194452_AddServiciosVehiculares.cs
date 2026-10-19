using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistLubri.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiciosVehiculares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiciosVehiculares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehiculoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Patente = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ClienteNombre = table.Column<string>(type: "nvarchar(201)", maxLength: 201, nullable: false),
                    KilometrajeIngreso = table.Column<int>(type: "int", nullable: false),
                    TrabajoSolicitado = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    InspeccionPrevia = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DanosExistentes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ManoObra = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreadoEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FinalizadoEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AnuladoEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiciosVehiculares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiciosVehiculares_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiciosVehiculares_Vehiculos_VehiculoId",
                        column: x => x.VehiculoId,
                        principalTable: "Vehiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesServicio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicioVehicularId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoProducto = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    NombreProducto = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    UnidadMedida = table.Column<int>(type: "int", nullable: false),
                    EsFraccionado = table.Column<bool>(type: "bit", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesServicio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesServicio_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesServicio_ServiciosVehiculares_ServicioVehicularId",
                        column: x => x.ServicioVehicularId,
                        principalTable: "ServiciosVehiculares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesServicio_ProductoId",
                table: "DetallesServicio",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesServicio_ServicioVehicularId_ProductoId",
                table: "DetallesServicio",
                columns: new[] { "ServicioVehicularId", "ProductoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiciosVehiculares_ClienteId",
                table: "ServiciosVehiculares",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiciosVehiculares_CreadoEnUtc",
                table: "ServiciosVehiculares",
                column: "CreadoEnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ServiciosVehiculares_Patente",
                table: "ServiciosVehiculares",
                column: "Patente");

            migrationBuilder.CreateIndex(
                name: "IX_ServiciosVehiculares_VehiculoId",
                table: "ServiciosVehiculares",
                column: "VehiculoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetallesServicio");

            migrationBuilder.DropTable(
                name: "ServiciosVehiculares");
        }
    }
}
