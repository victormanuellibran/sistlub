using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistLubri.Domain.Productos;

namespace SistLubri.Infrastructure.Persistence.Configurations;

internal sealed class MovimientoInventarioConfiguration : IEntityTypeConfiguration<MovimientoInventario>
{
    public void Configure(EntityTypeBuilder<MovimientoInventario> builder)
    {
        builder.ToTable("MovimientosInventario");
        builder.HasKey(movimiento => movimiento.Id);

        builder.Property(movimiento => movimiento.Tipo).HasConversion<string>().HasMaxLength(30);
        builder.Property(movimiento => movimiento.Cantidad).HasPrecision(18, 3);
        builder.Property(movimiento => movimiento.StockAnterior).HasPrecision(18, 3);
        builder.Property(movimiento => movimiento.StockResultante).HasPrecision(18, 3);
        builder.Property(movimiento => movimiento.Motivo).HasMaxLength(500).IsRequired();
        builder.HasIndex(movimiento => movimiento.UsuarioId);

        builder.HasIndex(movimiento => movimiento.ProductoId);
        builder.HasIndex(movimiento => movimiento.FechaUtc);
        builder.HasIndex(movimiento => movimiento.Tipo);
        builder.HasIndex(movimiento => movimiento.ReferenciaId);
    }
}
