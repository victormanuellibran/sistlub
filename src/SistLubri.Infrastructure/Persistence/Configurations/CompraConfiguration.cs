using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistLubri.Domain.Compras;

namespace SistLubri.Infrastructure.Persistence.Configurations;

internal sealed class CompraConfiguration : IEntityTypeConfiguration<Compra>
{
    public void Configure(EntityTypeBuilder<Compra> builder)
    {
        builder.ToTable("Compras");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Fecha).HasColumnType("date");
        builder.Property(item => item.Observaciones).HasMaxLength(1000);
        builder.Property(item => item.MotivoCierre).HasMaxLength(500);
        builder.Property(item => item.MotivoAnulacion).HasMaxLength(500);
        builder.Property(item => item.RowVersion).IsRowVersion();
        builder.Ignore(item => item.TotalEstimado);
        builder.Ignore(item => item.TotalFacturado);
        builder.Ignore(item => item.TotalPagado);
        builder.Ignore(item => item.SaldoPendiente);
        builder.Ignore(item => item.EstadoRecepcion);
        builder.Ignore(item => item.EstadoPago);
        builder.HasIndex(item => item.Fecha);
        builder.HasIndex(item => item.ProveedorId);
        builder.HasOne(item => item.Proveedor).WithMany().HasForeignKey(item => item.ProveedorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.Detalles).WithOne().HasForeignKey(item => item.CompraId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(item => item.Detalles).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(item => item.Recepciones).WithOne().HasForeignKey(item => item.CompraId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(item => item.Recepciones).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(item => item.Facturas).WithOne(item => item.Compra)
            .HasForeignKey(item => item.CompraId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(item => item.Facturas).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class DetalleCompraConfiguration : IEntityTypeConfiguration<DetalleCompra>
{
    public void Configure(EntityTypeBuilder<DetalleCompra> builder)
    {
        builder.ToTable("DetallesCompra");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.CodigoProducto).HasMaxLength(40).IsRequired();
        builder.Property(item => item.NombreProducto).HasMaxLength(120).IsRequired();
        builder.Property(item => item.UnidadMedida).HasMaxLength(20).IsRequired();
        builder.Property(item => item.CantidadSolicitada).HasPrecision(18, 3);
        builder.Property(item => item.CantidadAceptada).HasPrecision(18, 3);
        builder.Property(item => item.TotalRecepcionDanada).HasPrecision(18, 3);
        builder.Property(item => item.CostoUnitarioEstimado).HasPrecision(18, 2);
        builder.Ignore(item => item.CantidadPendiente);
        builder.Ignore(item => item.SubtotalEstimado);
        builder.HasOne<SistLubri.Domain.Productos.Producto>().WithMany().HasForeignKey(item => item.ProductoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.CompraId, item.ProductoId }).IsUnique();
    }
}

internal sealed class RecepcionCompraConfiguration : IEntityTypeConfiguration<RecepcionCompra>
{
    public void Configure(EntityTypeBuilder<RecepcionCompra> builder)
    {
        builder.ToTable("RecepcionesCompra");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Fecha).HasColumnType("date");
        builder.Property(item => item.Remito).HasMaxLength(120);
        builder.Property(item => item.Observaciones).HasMaxLength(1000);
        builder.HasIndex(item => new { item.CompraId, item.Fecha });
        builder.HasMany(item => item.Lineas).WithOne().HasForeignKey(item => item.RecepcionCompraId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(item => item.Lineas).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class LineaRecepcionCompraConfiguration : IEntityTypeConfiguration<LineaRecepcionCompra>
{
    public void Configure(EntityTypeBuilder<LineaRecepcionCompra> builder)
    {
        builder.ToTable("LineasRecepcionCompra");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.CantidadAceptada).HasPrecision(18, 3);
        builder.Property(item => item.CantidadDanada).HasPrecision(18, 3);
        builder.Property(item => item.CostoUnitario).HasPrecision(18, 2);
        builder.Property(item => item.Observaciones).HasMaxLength(500);
        builder.HasOne<DetalleCompra>().WithMany().HasForeignKey(item => item.DetalleCompraId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.RecepcionCompraId, item.DetalleCompraId }).IsUnique();
    }
}

internal sealed class FacturaCompraConfiguration : IEntityTypeConfiguration<FacturaCompra>
{
    public void Configure(EntityTypeBuilder<FacturaCompra> builder)
    {
        builder.ToTable("FacturasCompra");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Numero).HasMaxLength(120).IsRequired();
        builder.Property(item => item.Fecha).HasColumnType("date");
        builder.Property(item => item.FechaVencimiento).HasColumnType("date");
        builder.Property(item => item.ImporteTotal).HasPrecision(18, 2);
        builder.Property(item => item.Observaciones).HasMaxLength(1000);
        builder.Ignore(item => item.TotalPagado);
        builder.Ignore(item => item.SaldoPendiente);
        builder.HasIndex(item => new { item.CompraId, item.Numero }).IsUnique();
        builder.HasMany(item => item.Pagos).WithOne().HasForeignKey(item => item.FacturaCompraId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(item => item.Pagos).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class PagoCompraConfiguration : IEntityTypeConfiguration<PagoCompra>
{
    public void Configure(EntityTypeBuilder<PagoCompra> builder)
    {
        builder.ToTable("PagosCompra");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Fecha).HasColumnType("date");
        builder.Property(item => item.Importe).HasPrecision(18, 2);
        builder.Property(item => item.Referencia).HasMaxLength(120);
        builder.Property(item => item.Observacion).HasMaxLength(500);
        builder.HasIndex(item => new { item.FacturaCompraId, item.Fecha });
        builder.HasOne<SistLubri.Domain.Caja.MedioPago>().WithMany().HasForeignKey(item => item.MedioPagoId).OnDelete(DeleteBehavior.Restrict);
    }
}
