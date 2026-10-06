using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistLubri.Domain.Clientes;

namespace SistLubri.Infrastructure.Persistence.Configurations;

internal sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");
        builder.HasKey(cliente => cliente.Id);

        builder.Property(cliente => cliente.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(cliente => cliente.Apellido).HasMaxLength(100).IsRequired();
        builder.Property(cliente => cliente.Telefono).HasMaxLength(15).IsRequired();
        builder.Property(cliente => cliente.Email).HasMaxLength(254);
        builder.Property(cliente => cliente.Domicilio).HasMaxLength(250);
        builder.Property(cliente => cliente.Observaciones).HasMaxLength(1000);

        builder.HasIndex(cliente => cliente.Telefono);
        builder.HasIndex(cliente => cliente.Activo);
        builder.HasIndex(cliente => new { cliente.Nombre, cliente.Apellido, cliente.Telefono })
            .IsUnique();

        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}
