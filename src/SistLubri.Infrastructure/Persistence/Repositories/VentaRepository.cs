using Microsoft.EntityFrameworkCore;
using SistLubri.Application.Ventas;
using SistLubri.Domain.Ventas;

namespace SistLubri.Infrastructure.Persistence.Repositories;

internal sealed class VentaRepository(SistLubriDbContext dbContext) : IVentaRepository
{
    public Task<Venta?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Ventas.Include(sale => sale.Detalles).Include(sale => sale.Pagos).Include(sale => sale.Reintegros)
            .SingleOrDefaultAsync(sale => sale.Id == id, cancellationToken);

    public Task<Venta?> GetByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default) =>
        dbContext.Ventas.Include(sale => sale.Detalles).Include(sale => sale.Pagos).Include(sale => sale.Reintegros)
            .SingleOrDefaultAsync(sale => sale.ServicioId == serviceId, cancellationToken);

    public Task<bool> ExistsByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default) =>
        dbContext.Ventas.AnyAsync(sale => sale.ServicioId == serviceId, cancellationToken);

    public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Ventas.AnyAsync(sale => sale.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Venta>> SearchAsync(DateTimeOffset? desde, DateTimeOffset? hasta, Guid? clienteId, EstadoVenta? estado, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Ventas.AsNoTracking().AsQueryable();
        if (desde.HasValue) query = query.Where(sale => sale.CreadaEnUtc >= desde.Value);
        if (hasta.HasValue) query = query.Where(sale => sale.CreadaEnUtc <= hasta.Value);
        if (clienteId.HasValue) query = query.Where(sale => sale.ClienteId == clienteId.Value);
        if (estado.HasValue) query = query.Where(sale => sale.Estado == estado.Value);
        return await query.Include(sale => sale.Detalles).Include(sale => sale.Pagos).Include(sale => sale.Reintegros)
            .OrderByDescending(sale => sale.CreadaEnUtc).Take(200).ToListAsync(cancellationToken);
    }

    public void Add(Venta venta) => dbContext.Ventas.Add(venta);
}
