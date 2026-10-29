using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistLubri.Domain.Ventas;

namespace SistLubri.Infrastructure.Persistence.Configurations;

internal sealed class VentaConfiguration : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> builder)
    {
        builder.ToTable("Ventas");
        builder.HasKey(sale => sale.Id);
        builder.Property(sale => sale.Origen).HasConversion<string>().HasMaxLength(30);
        builder.Property(sale => sale.Estado).HasConversion<string>().HasMaxLength(30);
        builder.Property(sale => sale.ClienteNombre).HasMaxLength(201);
        builder.Property(sale => sale.Patente).HasMaxLength(10);
        builder.Property(sale => sale.ImporteManoObra).HasPrecision(18, 2);
        builder.Property(sale => sale.DescuentoGeneral).HasPrecision(18, 2);
        builder.Property(sale => sale.ReferenciaFacturaExterna).HasMaxLength(120);
        builder.Property(sale => sale.MotivoAnulacion).HasMaxLength(500);
        builder.Ignore(sale => sale.TotalBruto);
        builder.Ignore(sale => sale.DescuentoTotal);
        builder.Ignore(sale => sale.Total);
        builder.HasIndex("CreadaEnUtc");
        builder.HasIndex(sale => sale.ClienteId);
        builder.HasIndex(sale => sale.ServicioId).IsUnique().HasFilter("[ServicioId] IS NOT NULL");
        builder.HasMany(sale => sale.Detalles).WithOne(detail => detail.Venta).HasForeignKey(detail => detail.VentaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(sale => sale.Pagos).WithOne(payment => payment.Venta).HasForeignKey(payment => payment.VentaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(sale => sale.Reintegros).WithOne(refund => refund.Venta).HasForeignKey(refund => refund.VentaId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(sale => sale.Detalles).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(sale => sale.Pagos).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(sale => sale.Reintegros).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasOne<SistLubri.Domain.Clientes.Cliente>().WithMany().HasForeignKey(sale => sale.ClienteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SistLubri.Domain.Servicios.ServicioVehicular>().WithMany().HasForeignKey(sale => sale.ServicioId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(sale => sale.CreadaPorUsuarioId).IsRequired();
        builder.Property(sale => sale.ActualizadaPorUsuarioId).IsRequired();
        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}

internal sealed class DetalleVentaConfiguration : IEntityTypeConfiguration<DetalleVenta>
{
    public void Configure(EntityTypeBuilder<DetalleVenta> builder)
    {
        builder.ToTable("DetallesVenta");
        builder.HasKey(detail => detail.Id);
        builder.Property(detail => detail.CodigoProducto).HasMaxLength(40).IsRequired();
        builder.Property(detail => detail.NombreProducto).HasMaxLength(120).IsRequired();
        builder.Property(detail => detail.Presentacion).HasMaxLength(80).IsRequired();
        builder.Property(detail => detail.UnidadMedida).HasConversion<string>().HasMaxLength(20);
        builder.Property(detail => detail.Cantidad).HasPrecision(18, 3);
        builder.Property(detail => detail.CantidadDevuelta).HasPrecision(18, 3);
        builder.Property(detail => detail.PrecioUnitario).HasPrecision(18, 2);
        builder.Property(detail => detail.Descuento).HasPrecision(18, 2);
        builder.Property(detail => detail.Cantidad).IsRequired();
        builder.Ignore(detail => detail.SubtotalBruto);
        builder.Ignore(detail => detail.Subtotal);
        builder.HasIndex(detail => detail.ProductoId);
        builder.HasOne<SistLubri.Domain.Productos.Producto>().WithMany().HasForeignKey(detail => detail.ProductoId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PagoVentaConfiguration : IEntityTypeConfiguration<PagoVenta>
{
    public void Configure(EntityTypeBuilder<PagoVenta> builder)
    {
        builder.ToTable("PagosVenta");
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Importe).HasPrecision(18, 2);
        builder.Property(payment => payment.NombreMedioPago).HasMaxLength(60).IsRequired();
        builder.Property(payment => payment.TipoOperacion).HasMaxLength(20);
        builder.Property(payment => payment.EsReintegro).IsRequired();
        builder.Property(payment => payment.AfectaEfectivo).IsRequired();
        builder.Property(payment => payment.Referencia).HasMaxLength(120);
        builder.Property(payment => payment.Observacion).HasMaxLength(500);
        builder.HasIndex(payment => payment.MedioPagoId);
        builder.HasIndex(payment => payment.UsuarioId);
        builder.HasIndex(payment => payment.MovimientoCajaId).IsUnique().HasFilter("[MovimientoCajaId] IS NOT NULL");
        builder.Property(payment => payment.MovimientoCajaId).IsRequired(false);
    }
}

internal sealed class ReintegroVentaConfiguration : IEntityTypeConfiguration<ReintegroVenta>
{
    public void Configure(EntityTypeBuilder<ReintegroVenta> builder)
    {
        builder.ToTable("ReintegrosVenta");
        builder.HasKey(refund => refund.Id);
        builder.Property(refund => refund.Importe).HasPrecision(18, 2);
        builder.Property(refund => refund.NombreMedioPago).HasMaxLength(80).IsRequired();
        builder.Property(refund => refund.Referencia).HasMaxLength(120);
        builder.Property(refund => refund.Observacion).HasMaxLength(500);
        builder.Property(refund => refund.AfectaEfectivo).IsRequired();
        builder.Property(refund => refund.EsAnulacion).IsRequired();
        builder.HasIndex(refund => refund.MedioPagoId);
        builder.HasIndex(refund => refund.UsuarioId);
        builder.HasIndex(refund => refund.MovimientoCajaId).IsUnique().HasFilter("[MovimientoCajaId] IS NOT NULL");
    }
}
