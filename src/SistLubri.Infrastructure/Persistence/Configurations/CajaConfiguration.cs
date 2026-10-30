using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistLubri.Domain.Caja;

namespace SistLubri.Infrastructure.Persistence.Configurations;

internal sealed class JornadaCajaConfiguration : IEntityTypeConfiguration<JornadaCaja>
{
    public void Configure(EntityTypeBuilder<JornadaCaja> builder)
    {
        builder.ToTable("JornadasCaja");
        builder.HasKey(jornada => jornada.Id);
        builder.Property(jornada => jornada.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(jornada => jornada.SaldoInicial).HasPrecision(18, 2);
        builder.Property<byte[]>("RowVersion").IsRowVersion().IsRequired();
        builder.Property(jornada => jornada.SaldoContado).HasPrecision(18, 2);
        builder.Property(jornada => jornada.DiferenciaVerificacion).HasPrecision(18, 2);
        builder.Property(jornada => jornada.ObservacionVerificacion).HasMaxLength(500);
        builder.Property(jornada => jornada.MotivoReapertura).HasMaxLength(500);
        builder.Ignore(jornada => jornada.TotalIngresos);
        builder.Ignore(jornada => jornada.TotalEgresos);
        builder.Ignore(jornada => jornada.SaldoEsperado);
        builder.Ignore(jornada => jornada.EfectivoEsperado);
        builder.Ignore(jornada => jornada.MovimientosPendientesPersistencia);
        builder.Ignore(jornada => jornada.CierresPendientesPersistencia);
        builder.Ignore(jornada => jornada.AperturasPendientesPersistencia);
        builder.HasIndex(jornada => jornada.Fecha).IsUnique();
        builder.HasMany(jornada => jornada.Movimientos).WithOne(movement => movement.JornadaCaja).HasForeignKey(movement => movement.JornadaCajaId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(jornada => jornada.Movimientos).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(jornada => jornada.Cierres).WithOne(close => close.JornadaCaja)
            .HasForeignKey(close => close.JornadaCajaId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(jornada => jornada.Cierres).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(jornada => jornada.Aperturas).WithOne(open => open.JornadaCaja)
            .HasForeignKey(open => open.JornadaCajaId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(jornada => jornada.Aperturas).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class MovimientoCajaConfiguration : IEntityTypeConfiguration<MovimientoCaja>
{
    public void Configure(EntityTypeBuilder<MovimientoCaja> builder)
    {
        builder.ToTable("MovimientosCaja");
        builder.HasKey(movement => movement.Id);
        builder.Property(movement => movement.Tipo).HasConversion<string>().HasMaxLength(30);
        builder.Property(movement => movement.Importe).HasPrecision(18, 2);
        builder.Property(movement => movement.AfectaEfectivo).IsRequired();
        builder.Property(movement => movement.Referencia).HasMaxLength(120);
        builder.Property(movement => movement.Motivo).HasMaxLength(500);
        builder.HasIndex(movement => movement.FechaUtc);
        builder.HasIndex(movement => movement.VentaId);
        builder.HasIndex(movement => movement.MedioPagoId);
        builder.HasIndex(movement => movement.PagoVentaId).IsUnique().HasFilter("[PagoVentaId] IS NOT NULL");
        builder.HasIndex(movement => movement.PagoGastoId).IsUnique().HasFilter("[PagoGastoId] IS NOT NULL");
        builder.HasIndex(movement => movement.ReintegroGastoId).IsUnique().HasFilter("[ReintegroGastoId] IS NOT NULL");
        builder.HasIndex(movement => movement.FacturaCompraId);
        builder.HasIndex(movement => movement.PagoCompraId).IsUnique().HasFilter("[PagoCompraId] IS NOT NULL");
        builder.HasOne<SistLubri.Domain.Gastos.Gasto>().WithMany().HasForeignKey(movement => movement.GastoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SistLubri.Domain.Compras.FacturaCompra>().WithMany().HasForeignKey(movement => movement.FacturaCompraId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(movement => movement.PagoCompra).WithOne().HasForeignKey<MovimientoCaja>(movement => movement.PagoCompraId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MedioPagoConfiguration : IEntityTypeConfiguration<MedioPago>
{
    public void Configure(EntityTypeBuilder<MedioPago> builder)
    {
        builder.ToTable("MediosPago");
        builder.HasKey(method => method.Id);
        builder.Property(method => method.Nombre).HasMaxLength(60).IsRequired();
        builder.HasIndex(method => method.Nombre).IsUnique();
    }
}

internal sealed class ConfiguracionCajaConfiguration : IEntityTypeConfiguration<ConfiguracionCaja>
{
    public void Configure(EntityTypeBuilder<ConfiguracionCaja> builder)
    {
        builder.ToTable("ConfiguracionCaja");
        builder.HasKey(configuration => configuration.Id);
        builder.Property(configuration => configuration.HoraApertura).HasColumnType("time");
        builder.Property(configuration => configuration.HoraCierre).HasColumnType("time");
        builder.Property(configuration => configuration.DuracionJornada).HasColumnType("time");
        builder.HasIndex(configuration => configuration.Id).IsUnique();
    }
}

internal sealed class RegistroCierreCajaConfiguration : IEntityTypeConfiguration<RegistroCierreCaja>
{
    public void Configure(EntityTypeBuilder<RegistroCierreCaja> builder)
    {
        builder.ToTable("RegistrosCierreCaja");
        builder.HasKey(close => close.Id);
        builder.Property(close => close.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(close => close.EfectivoEsperado).HasPrecision(18, 2);
        builder.Property(close => close.EfectivoContado).HasPrecision(18, 2);
        builder.Property(close => close.Diferencia).HasPrecision(18, 2);
        builder.Property(close => close.Observacion).HasMaxLength(500);
        builder.Property(close => close.MotivoReapertura).HasMaxLength(500);
        builder.HasIndex(close => close.ProximoCierreId).IsUnique().HasFilter("[ProximoCierreId] IS NOT NULL");
        builder.HasIndex(close => new { close.JornadaCajaId, close.Numero }).IsUnique();
    }
}

internal sealed class RegistroAperturaCajaConfiguration : IEntityTypeConfiguration<RegistroAperturaCaja>
{
    public void Configure(EntityTypeBuilder<RegistroAperturaCaja> builder)
    {
        builder.ToTable("RegistrosAperturaCaja");
        builder.HasKey(open => open.Id);
        builder.Property(open => open.SaldoInicial).HasPrecision(18, 2);
        builder.HasIndex(open => new { open.JornadaCajaId, open.Numero }).IsUnique();
    }
}
