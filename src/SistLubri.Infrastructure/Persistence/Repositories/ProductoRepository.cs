using Microsoft.EntityFrameworkCore;
using SistLubri.Application.Productos;
using SistLubri.Domain.Productos;

namespace SistLubri.Infrastructure.Persistence.Repositories;

internal sealed class ProductoRepository(SistLubriDbContext dbContext) : IProductoRepository
{
    public Task<Producto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Productos.SingleOrDefaultAsync(producto => producto.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Producto>> SearchAsync(
        string? search,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Productos.AsNoTracking().AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(producto => producto.Activo);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var code = term.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
            query = query.Where(producto =>
                producto.Codigo.Contains(code) ||
                producto.Nombre.Contains(term) ||
                (producto.Marca != null && producto.Marca.Contains(term)) ||
                (producto.Categoria != null && producto.Categoria.Contains(term)) ||
                producto.Presentacion.Contains(term));
        }

        return await query
            .OrderBy(producto => producto.Nombre)
            .ThenBy(producto => producto.Presentacion)
            .Take(200)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, DateTimeOffset>> GetLatestMovementDatesAsync(
        IReadOnlyCollection<Guid> productoIds,
        CancellationToken cancellationToken = default)
    {
        if (productoIds.Count == 0)
            return new Dictionary<Guid, DateTimeOffset>();

        var dates = await dbContext.MovimientosInventario
            .AsNoTracking()
            .Where(movimiento => productoIds.Contains(movimiento.ProductoId))
            .GroupBy(movimiento => movimiento.ProductoId)
            .Select(group => new { ProductoId = group.Key, Fecha = group.Max(movimiento => movimiento.FechaUtc) })
            .ToDictionaryAsync(item => item.ProductoId, item => item.Fecha, cancellationToken);
        return dates;
    }

    public async Task<IReadOnlyList<Producto>> GetAlertsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Productos
            .AsNoTracking()
            .Where(producto =>
                producto.Activo &&
                (producto.StockActual - producto.StockReservado <= 0 ||
                    (producto.StockMinimo > 0 && producto.StockActual - producto.StockReservado <= producto.StockMinimo)))
            .OrderBy(producto => producto.StockActual > 0)
            .ThenBy(producto => producto.Nombre)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MovimientoInventario>> GetMovementsAsync(
        Guid productoId,
        TipoMovimientoInventario? tipo,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.MovimientosInventario
            .AsNoTracking()
            .Include(movimiento => movimiento.Producto)
            .Where(movimiento => movimiento.ProductoId == productoId);

        if (tipo.HasValue)
        {
            query = query.Where(movimiento => movimiento.Tipo == tipo.Value);
        }

        return await query
            .OrderByDescending(movimiento => movimiento.FechaUtc)
            .Take(200)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MovimientoInventario>> SearchMovementsAsync(
        Guid? productoId,
        DateOnly? desde,
        DateOnly? hasta,
        TipoMovimientoInventario? tipo,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.MovimientosInventario
            .AsNoTracking()
            .Include(movimiento => movimiento.Producto)
            .AsQueryable();

        if (productoId.HasValue)
            query = query.Where(movimiento => movimiento.ProductoId == productoId.Value);
        if (desde.HasValue)
            query = query.Where(movimiento => movimiento.FechaUtc >= desde.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        if (hasta.HasValue)
            query = query.Where(movimiento => movimiento.FechaUtc < hasta.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        if (tipo.HasValue)
            query = query.Where(movimiento => movimiento.Tipo == tipo.Value);

        return await query.OrderByDescending(movimiento => movimiento.FechaUtc)
            .Take(500)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsCodeAsync(
        string codigo,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Productos.AnyAsync(
            producto =>
                producto.Codigo == codigo &&
                (!excludingId.HasValue || producto.Id != excludingId.Value),
            cancellationToken);
    }

    public void Add(Producto producto)
    {
        dbContext.Productos.Add(producto);
    }

    public void AddMovement(MovimientoInventario movimiento)
    {
        dbContext.MovimientosInventario.Add(movimiento);
    }
}
