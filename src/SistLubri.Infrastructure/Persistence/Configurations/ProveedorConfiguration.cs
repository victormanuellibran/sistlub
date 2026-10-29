using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistLubri.Domain.Proveedores;

namespace SistLubri.Infrastructure.Persistence.Configurations;

internal sealed class ProveedorConfiguration : IEntityTypeConfiguration<Proveedor>
{
    public void Configure(EntityTypeBuilder<Proveedor> builder)
    {
        builder.ToTable("Proveedores");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.RazonSocial).HasMaxLength(160).IsRequired();
        builder.Property(item => item.Cuit).HasMaxLength(11);
        builder.Property(item => item.ContactoNombre).HasMaxLength(120);
        builder.Property(item => item.Telefono).HasMaxLength(15).IsRequired();
        builder.Property(item => item.Email).HasMaxLength(254);
        builder.Property(item => item.Direccion).HasMaxLength(250);
        builder.Property(item => item.MarcasYProductos).HasMaxLength(500);
        builder.Property(item => item.CondicionPagoHabitual).HasMaxLength(120);
        builder.Property(item => item.PeriodoPagoHabitual).HasMaxLength(80);
        builder.Property(item => item.Observaciones).HasMaxLength(1000);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Ignore(item => item.HistorialEstadosPendientesPersistencia);
        builder.HasIndex(item => item.Cuit).IsUnique().HasFilter("[Cuit] IS NOT NULL");
        builder.HasIndex(item => item.Activo);
        builder.HasIndex(item => item.RazonSocial);
        builder.HasIndex(item => item.Telefono);
        builder.HasMany(item => item.HistorialEstados)
            .WithOne(entry => entry.Proveedor)
            .HasForeignKey(entry => entry.ProveedorId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(item => item.HistorialEstados).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class RegistroEstadoProveedorConfiguration : IEntityTypeConfiguration<RegistroEstadoProveedor>
{
    public void Configure(EntityTypeBuilder<RegistroEstadoProveedor> builder)
    {
        builder.ToTable("RegistrosEstadoProveedor");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(entry => entry.Motivo).HasMaxLength(500);
        builder.HasIndex(entry => new { entry.ProveedorId, entry.FechaUtc });
    }
}
