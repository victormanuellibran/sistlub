using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistLubri.Domain.Vehiculos;

namespace SistLubri.Infrastructure.Persistence.Configurations;

internal sealed class VehiculoConfiguration : IEntityTypeConfiguration<Vehiculo>
{
    public void Configure(EntityTypeBuilder<Vehiculo> builder)
    {
        builder.ToTable("Vehiculos");
        builder.HasKey(vehiculo => vehiculo.Id);

        builder.Property(vehiculo => vehiculo.Patente).HasMaxLength(10).IsRequired();
        builder.Property(vehiculo => vehiculo.Marca).HasMaxLength(80).IsRequired();
        builder.Property(vehiculo => vehiculo.Modelo).HasMaxLength(80).IsRequired();
        builder.Property(vehiculo => vehiculo.Observaciones).HasMaxLength(1000);

        builder.HasIndex(vehiculo => vehiculo.Patente).IsUnique();
        builder.HasIndex(vehiculo => vehiculo.ClienteId);

        builder.HasOne(vehiculo => vehiculo.Cliente)
            .WithMany()
            .HasForeignKey(vehiculo => vehiculo.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}
