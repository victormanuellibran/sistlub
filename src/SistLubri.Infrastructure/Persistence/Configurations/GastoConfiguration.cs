using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistLubri.Domain.Caja;
using SistLubri.Domain.Gastos;

namespace SistLubri.Infrastructure.Persistence.Configurations;

internal sealed class GastoConfiguration : IEntityTypeConfiguration<Gasto>
{
    public void Configure(EntityTypeBuilder<Gasto> builder)
    {
        builder.ToTable("Gastos");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Categoria).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(item => item.CategoriaDetalle).HasMaxLength(120);
        builder.Property(item => item.Descripcion).HasMaxLength(500).IsRequired();
        builder.Property(item => item.ImporteTotal).HasPrecision(18, 2);
        builder.Property(item => item.Estado).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(item => item.EntidadRelacionada).HasMaxLength(200);
        builder.Property(item => item.ReferenciaComprobante).HasMaxLength(120);
        builder.Property(item => item.Observaciones).HasMaxLength(1000);
        builder.Property(item => item.MotivoAnulacion).HasMaxLength(500);
        builder.Property(item => item.RowVersion).IsRowVersion();
        builder.Ignore(item => item.TotalPagado);
        builder.Ignore(item => item.TotalReintegrado);
        builder.Ignore(item => item.SaldoPendiente);
        builder.Ignore(item => item.PagosPendientesPersistencia);
        builder.Ignore(item => item.ReintegrosPendientesPersistencia);
        builder.HasIndex(item => item.Fecha);
        builder.HasIndex(item => item.Estado);
        builder.HasIndex(item => item.Categoria);
        builder.HasIndex(item => item.FechaVencimiento);
        builder.HasIndex(item => item.ProveedorId);
        builder.HasIndex(item => item.CreadoPorUsuarioId);
        builder.HasOne(item => item.Proveedor).WithMany().HasForeignKey(item => item.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.Pagos).WithOne().HasForeignKey(payment => payment.GastoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(item => item.Pagos).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(item => item.Reintegros).WithOne().HasForeignKey(refund => refund.GastoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(item => item.Reintegros).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class PagoGastoConfiguration : IEntityTypeConfiguration<PagoGasto>
{
    public void Configure(EntityTypeBuilder<PagoGasto> builder)
    {
        builder.ToTable("PagosGasto");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Importe).HasPrecision(18, 2);
        builder.Property(item => item.Referencia).HasMaxLength(120);
        builder.Property(item => item.Observacion).HasMaxLength(500);
        builder.Ignore(item => item.MovimientoCajaId);
        builder.HasIndex(item => new { item.GastoId, item.Fecha });
        builder.HasOne(item => item.MovimientoCaja).WithOne(movement => movement.PagoGasto)
            .HasForeignKey<MovimientoCaja>(movement => movement.PagoGastoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ReintegroGastoConfiguration : IEntityTypeConfiguration<ReintegroGasto>
{
    public void Configure(EntityTypeBuilder<ReintegroGasto> builder)
    {
        builder.ToTable("ReintegrosGasto");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Importe).HasPrecision(18, 2);
        builder.Property(item => item.Referencia).HasMaxLength(120);
        builder.Property(item => item.Observacion).HasMaxLength(500);
        builder.Ignore(item => item.MovimientoCajaId);
        builder.HasIndex(item => new { item.GastoId, item.Fecha });
        builder.HasOne(item => item.MovimientoCaja).WithOne(movement => movement.ReintegroGasto)
            .HasForeignKey<MovimientoCaja>(movement => movement.ReintegroGastoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
