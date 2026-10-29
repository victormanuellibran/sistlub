using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistLubri.Domain.Clientes;
using SistLubri.Domain.Servicios;
using SistLubri.Domain.Vehiculos;
using SistLubri.Domain.Productos;

namespace SistLubri.Infrastructure.Persistence.Configurations;

internal sealed class ServicioVehicularConfiguration : IEntityTypeConfiguration<ServicioVehicular>
{
    public void Configure(EntityTypeBuilder<ServicioVehicular> builder)
    {
        builder.ToTable("ServiciosVehiculares");
        builder.HasKey(servicio => servicio.Id);
        builder.Property(servicio => servicio.Patente).HasMaxLength(10).IsRequired();
        builder.Property(servicio => servicio.ClienteNombre).HasMaxLength(201).IsRequired();
        builder.Property(servicio => servicio.TrabajoSolicitado).HasMaxLength(500).IsRequired();
        builder.Property(servicio => servicio.InspeccionPrevia).HasMaxLength(2000);
        builder.Property(servicio => servicio.DanosExistentes).HasMaxLength(2000);
        builder.Property(servicio => servicio.Observaciones).HasMaxLength(2000);
        builder.Property(servicio => servicio.MotivoAnulacion).HasMaxLength(500);
        builder.Property(servicio => servicio.CreadoPorUsuarioId);
        builder.Property(servicio => servicio.ActualizadoPorUsuarioId);
        builder.Property(servicio => servicio.ActualizadoEnUtc);
        builder.Property(servicio => servicio.FinalizadoPorUsuarioId);
        builder.Property(servicio => servicio.AnuladoPorUsuarioId);
        builder.Property(servicio => servicio.ManoObra).HasPrecision(18, 2);
        builder.Property(servicio => servicio.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.HasIndex(servicio => servicio.VehiculoId);
        builder.HasIndex(servicio => servicio.ClienteId);
        builder.HasIndex(servicio => servicio.Patente);
        builder.HasIndex(servicio => servicio.CreadoEnUtc);
        builder.HasIndex(servicio => servicio.Estado);
        builder.HasOne<Vehiculo>()
            .WithMany()
            .HasForeignKey(servicio => servicio.VehiculoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(servicio => servicio.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(servicio => servicio.Detalles)
            .WithOne(detalle => detalle.ServicioVehicular)
            .HasForeignKey(detalle => detalle.ServicioVehicularId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(servicio => servicio.Detalles).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class DetalleServicioConfiguration : IEntityTypeConfiguration<DetalleServicio>
{
    public void Configure(EntityTypeBuilder<DetalleServicio> builder)
    {
        builder.ToTable("DetallesServicio");
        builder.HasKey(detalle => detalle.Id);
        builder.Property(detalle => detalle.CodigoProducto).HasMaxLength(40).IsRequired();
        builder.Property(detalle => detalle.NombreProducto).HasMaxLength(120).IsRequired();
        builder.Property(detalle => detalle.Cantidad).HasPrecision(18, 3);
        builder.Property(detalle => detalle.PrecioUnitario).HasPrecision(18, 2);
        builder.Property(detalle => detalle.CostoUnitario).HasPrecision(18, 2);
        builder.Property(detalle => detalle.UnidadMedida).HasConversion<string>().HasMaxLength(20);
        builder.Ignore(detalle => detalle.Subtotal);
        builder.HasIndex(detalle => detalle.ProductoId);
        builder.HasIndex(detalle => new { detalle.ServicioVehicularId, detalle.ProductoId }).IsUnique();
        builder.HasOne<Producto>()
            .WithMany()
            .HasForeignKey(detalle => detalle.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
