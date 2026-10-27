using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistLubri.Domain.Productos;

namespace SistLubri.Infrastructure.Persistence.Configurations;

internal sealed class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.ToTable("Productos");
        builder.HasKey(producto => producto.Id);

        builder.Property(producto => producto.Codigo).HasMaxLength(40).IsRequired();
        builder.Property(producto => producto.Nombre).HasMaxLength(120).IsRequired();
        builder.Property(producto => producto.Marca).HasMaxLength(80);
        builder.Property(producto => producto.Categoria).HasMaxLength(80);
        builder.Property(producto => producto.Presentacion).HasMaxLength(80).IsRequired();
        builder.Property(producto => producto.UnidadMedida).HasConversion<string>().HasMaxLength(20);
        builder.Property(producto => producto.PrecioVenta).HasPrecision(18, 2);
        builder.Property(producto => producto.CostoCompra).HasPrecision(18, 2);
        builder.Property(producto => producto.StockActual).HasPrecision(18, 3);
        builder.Property(producto => producto.StockMinimo).HasPrecision(18, 3);

        builder.Ignore(producto => producto.EstadoInventario);

        builder.HasIndex(producto => producto.Codigo).IsUnique();
        builder.HasIndex(producto => producto.Nombre);
        builder.HasIndex(producto => producto.Marca);
        builder.HasIndex(producto => producto.Categoria);

        builder.HasMany(producto => producto.Movimientos)
            .WithOne(movimiento => movimiento.Producto)
            .HasForeignKey(movimiento => movimiento.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(producto => producto.Movimientos)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}
