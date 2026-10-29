using Microsoft.EntityFrameworkCore;
using SistLubri.Application.Caja;
using SistLubri.Domain.Caja;

namespace SistLubri.Infrastructure.Persistence.Repositories;

internal sealed class CajaRepository(SistLubriDbContext dbContext) : ICajaRepository
{
    public async Task<JornadaCaja?> GetActualAsync(CancellationToken cancellationToken = default)
    {
        var day = await dbContext.JornadasCaja.Include(item => item.Movimientos).Include(item => item.Cierres).Include(item => item.Aperturas)
            .SingleOrDefaultAsync(item => item.Estado == EstadoJornadaCaja.Abierta || item.Estado == EstadoJornadaCaja.Reabierta, cancellationToken);
        return day;
    }

    public async Task<JornadaCaja?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var day = await dbContext.JornadasCaja.Include(item => item.Movimientos).Include(item => item.Cierres).Include(item => item.Aperturas)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return day;
    }

    public async Task<JornadaCaja?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var day = await dbContext.JornadasCaja.Include(item => item.Movimientos).Include(item => item.Cierres).Include(item => item.Aperturas)
            .SingleOrDefaultAsync(item => item.Fecha == date, cancellationToken);
        return day;
    }

    public async Task<JornadaCaja?> GetMostRecentBeforeAsync(DateOnly date, CancellationToken cancellationToken = default) =>
        await dbContext.JornadasCaja
            .Include(item => item.Movimientos)
            .Include(item => item.Cierres)
            .Include(item => item.Aperturas)
            .Where(item => item.Fecha < date)
            .OrderByDescending(item => item.Fecha)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<JornadaCaja>> SearchAsync(DateOnly? desde, DateOnly? hasta, CancellationToken cancellationToken = default)
    {
        var query = dbContext.JornadasCaja.AsNoTracking().AsQueryable();
        if (desde.HasValue) query = query.Where(day => day.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(day => day.Fecha <= hasta.Value);
        var days = await query.Include(day => day.Movimientos).Include(day => day.Cierres).Include(day => day.Aperturas).OrderByDescending(day => day.Fecha).Take(200).ToListAsync(cancellationToken);
        return days;
    }

    public Task<ConfiguracionCaja?> GetConfigurationAsync(CancellationToken cancellationToken = default) =>
        dbContext.ConfiguracionesCaja.FirstOrDefaultAsync(cancellationToken);

    public Task<bool> HasOpenAsync(CancellationToken cancellationToken = default) =>
        dbContext.JornadasCaja.AnyAsync(day => day.Estado == EstadoJornadaCaja.Abierta || day.Estado == EstadoJornadaCaja.Reabierta, cancellationToken);

    public Task<bool> ExistsOnDateAsync(DateOnly date, CancellationToken cancellationToken = default) =>
        dbContext.JornadasCaja.AnyAsync(day => day.Fecha == date, cancellationToken);

    public Task<bool> ExistsAnyAsync(CancellationToken cancellationToken = default) =>
        dbContext.JornadasCaja.AnyAsync(cancellationToken);

    public async Task<IReadOnlyList<MedioPago>> GetMediosPagoAsync(CancellationToken cancellationToken = default) =>
        await dbContext.MediosPago.AsNoTracking().OrderBy(method => method.Nombre).ToListAsync(cancellationToken);

    public Task<MedioPago?> GetMedioPagoAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.MediosPago.SingleOrDefaultAsync(method => method.Id == id, cancellationToken);

    public Task<bool> ExistsPaymentMethodNameAsync(string nombre, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        dbContext.MediosPago.AnyAsync(method => method.Id != excludingId && method.Nombre.ToLower() == nombre.Trim().ToLower(), cancellationToken);

    public void Add(JornadaCaja jornada) => dbContext.JornadasCaja.Add(jornada);
    public void Add(MovimientoCaja movimiento) => dbContext.MovimientosCaja.Add(movimiento);
    public void Add(MedioPago medioPago) => dbContext.MediosPago.Add(medioPago);
    public void Add(ConfiguracionCaja configuration) => dbContext.ConfiguracionesCaja.Add(configuration);
    public void Add(RegistroCierreCaja cierre) => dbContext.RegistrosCierreCaja.Add(cierre);
    public void Add(RegistroAperturaCaja apertura) => dbContext.RegistrosAperturaCaja.Add(apertura);

}
