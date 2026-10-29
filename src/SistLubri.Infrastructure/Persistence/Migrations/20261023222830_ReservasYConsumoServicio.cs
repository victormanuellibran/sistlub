using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistLubri.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReservasYConsumoServicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "StockReservado",
                table: "Productos",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ActualizadoEnUtc",
                table: "ServiciosVehiculares",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ActualizadoPorUsuarioId",
                table: "ServiciosVehiculares",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ServiciosVehiculares",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_ServiciosVehiculares_Estado",
                table: "ServiciosVehiculares",
                column: "Estado");

            migrationBuilder.Sql("""
                UPDATE producto
                SET StockReservado = reservas.Cantidad
                FROM Productos AS producto
                INNER JOIN (
                    SELECT detalle.ProductoId, SUM(detalle.Cantidad) AS Cantidad
                    FROM DetallesServicio AS detalle
                    INNER JOIN ServiciosVehiculares AS servicio
                        ON servicio.Id = detalle.ServicioVehicularId
                    WHERE servicio.Estado = 'EnCurso'
                    GROUP BY detalle.ProductoId
                ) AS reservas ON reservas.ProductoId = producto.Id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiciosVehiculares_Estado",
                table: "ServiciosVehiculares");

            migrationBuilder.DropColumn(name: "StockReservado", table: "Productos");
            migrationBuilder.DropColumn(name: "ActualizadoEnUtc", table: "ServiciosVehiculares");
            migrationBuilder.DropColumn(name: "ActualizadoPorUsuarioId", table: "ServiciosVehiculares");
            migrationBuilder.DropColumn(name: "RowVersion", table: "ServiciosVehiculares");
        }
    }
}
