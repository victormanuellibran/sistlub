using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistLubri.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CascadeServicioDetalles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DetallesServicio_ServiciosVehiculares_ServicioVehicularId",
                table: "DetallesServicio");

            migrationBuilder.AddForeignKey(
                name: "FK_DetallesServicio_ServiciosVehiculares_ServicioVehicularId",
                table: "DetallesServicio",
                column: "ServicioVehicularId",
                principalTable: "ServiciosVehiculares",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DetallesServicio_ServiciosVehiculares_ServicioVehicularId",
                table: "DetallesServicio");

            migrationBuilder.AddForeignKey(
                name: "FK_DetallesServicio_ServiciosVehiculares_ServicioVehicularId",
                table: "DetallesServicio",
                column: "ServicioVehicularId",
                principalTable: "ServiciosVehiculares",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
