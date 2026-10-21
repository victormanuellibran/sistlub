using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistLubri.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialClientesVehiculos : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Clientes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Telefono = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                Domicilio = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                Activo = table.Column<bool>(type: "bit", nullable: false),
                CreadoEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ActualizadoEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                DesactivadoEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Clientes", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Vehiculos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ClienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Patente = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                Marca = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Modelo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Anio = table.Column<int>(type: "int", nullable: true),
                KilometrajeActual = table.Column<int>(type: "int", nullable: true),
                Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                CreadoEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ActualizadoEnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Vehiculos", x => x.Id);
                table.ForeignKey(
                    name: "FK_Vehiculos_Clientes_ClienteId",
                    column: x => x.ClienteId,
                    principalTable: "Clientes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Clientes_Activo",
            table: "Clientes",
            column: "Activo");

        migrationBuilder.CreateIndex(
            name: "IX_Clientes_Nombre_Apellido_Telefono",
            table: "Clientes",
            columns: new[] { "Nombre", "Apellido", "Telefono" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Clientes_Telefono",
            table: "Clientes",
            column: "Telefono");

        migrationBuilder.CreateIndex(
            name: "IX_Vehiculos_ClienteId",
            table: "Vehiculos",
            column: "ClienteId");

        migrationBuilder.CreateIndex(
            name: "IX_Vehiculos_Patente",
            table: "Vehiculos",
            column: "Patente",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Vehiculos");

        migrationBuilder.DropTable(
            name: "Clientes");
    }
}
