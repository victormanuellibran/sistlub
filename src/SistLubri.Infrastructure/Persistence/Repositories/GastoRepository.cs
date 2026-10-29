using Microsoft.EntityFrameworkCore;
using SistLubri.Application.Gastos;
using SistLubri.Domain.Gastos;

namespace SistLubri.Infrastructure.Persistence.Repositories;

internal sealed class GastoRepository(SistLubriDbContext dbContext) : IGastoRepository
{
    public Task<Gasto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        BuildQuery(dbContext.Gastos).SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Gasto>> SearchAsync(
        FiltroGastos filtro,
        DateOnly hoy,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Gastos.AsNoTracking().AsQueryable();
        if (filtro.Desde.HasValue) query = query.Where(item => item.Fecha >= filtro.Desde.Value);
        if (filtro.Hasta.HasValue) query = query.Where(item => item.Fecha <= filtro.Hasta.Value);
        if (filtro.VencimientoDesde.HasValue) query = query.Where(item => item.FechaVencimiento >= filtro.VencimientoDesde.Value);
        if (filtro.VencimientoHasta.HasValue) query = query.Where(item => item.FechaVencimiento <= filtro.VencimientoHasta.Value);
        if (filtro.Categoria.HasValue) query = query.Where(item => item.Categoria == filtro.Categoria.Value);
        if (filtro.Estado.HasValue) query = query.Where(item => item.Estado == filtro.Estado.Value);
        if (filtro.MedioPagoId.HasValue) query = query.Where(item => item.Pagos.Any(payment => payment.MedioPagoId == filtro.MedioPagoId.Value));
        if (filtro.ProveedorId.HasValue) query = query.Where(item => item.ProveedorId == filtro.ProveedorId.Value);
        if (filtro.ImporteMinimo.HasValue) query = query.Where(item => item.ImporteTotal >= filtro.ImporteMinimo.Value);
        if (filtro.ImporteMaximo.HasValue) query = query.Where(item => item.ImporteTotal <= filtro.ImporteMaximo.Value);
        if (filtro.UsuarioId.HasValue) query = query.Where(item => item.CreadoPorUsuarioId == filtro.UsuarioId.Value);

        if (!string.IsNullOrWhiteSpace(filtro.Entidad))
        {
            var entity = filtro.Entidad.Trim();
            query = query.Where(item => (item.EntidadRelacionada != null && item.EntidadRelacionada.Contains(entity)) ||
                (item.Proveedor != null && item.Proveedor.RazonSocial.Contains(entity)));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Buscar))
        {
            var term = filtro.Buscar.Trim();
            query = query.Where(item => item.Descripcion.Contains(term) ||
                (item.EntidadRelacionada != null && item.EntidadRelacionada.Contains(term)) ||
                (item.ReferenciaComprobante != null && item.ReferenciaComprobante.Contains(term)) ||
                (item.Proveedor != null && item.Proveedor.RazonSocial.Contains(term)));
        }

        if (filtro.Situacion.HasValue)
        {
            var nearDue = hoy.AddDays(7);
            query = filtro.Situacion.Value switch
            {
                SituacionGasto.Pendiente => query.Where(item => item.Estado == EstadoGasto.Pendiente &&
                    (!item.FechaVencimiento.HasValue || item.FechaVencimiento.Value > nearDue)),
                SituacionGasto.ProximoAVencer => query.Where(item =>
                    (item.Estado == EstadoGasto.Pendiente || item.Estado == EstadoGasto.ParcialmentePagado) &&
                    item.FechaVencimiento >= hoy && item.FechaVencimiento <= nearDue),
                SituacionGasto.Vencido => query.Where(item =>
                    (item.Estado == EstadoGasto.Pendiente || item.Estado == EstadoGasto.ParcialmentePagado) &&
                    item.FechaVencimiento < hoy),
                SituacionGasto.ParcialmentePagado => query.Where(item =>
                    item.Estado == EstadoGasto.ParcialmentePagado &&
                    (!item.FechaVencimiento.HasValue || item.FechaVencimiento.Value > nearDue)),
                SituacionGasto.Pagado => query.Where(item => item.Estado == EstadoGasto.Pagado),
                SituacionGasto.Anulado => query.Where(item => item.Estado == EstadoGasto.Anulado),
                _ => query
            };
        }

        return await BuildQuery(query).OrderByDescending(item => item.Fecha)
            .ThenByDescending(item => item.CreadoEnUtc).Take(200).ToListAsync(cancellationToken);
    }

    public void Add(Gasto gasto) => dbContext.Gastos.Add(gasto);
    public void Add(PagoGasto pago) => dbContext.PagosGasto.Add(pago);
    public void Add(ReintegroGasto reintegro) => dbContext.ReintegrosGasto.Add(reintegro);

    private static IQueryable<Gasto> BuildQuery(IQueryable<Gasto> query) => query
        .Include(item => item.Proveedor)
        .Include(item => item.Pagos).ThenInclude(payment => payment.MovimientoCaja)
        .Include(item => item.Reintegros).ThenInclude(refund => refund.MovimientoCaja);
}
