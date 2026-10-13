using SistLubri.Domain.Productos;

namespace SistLubri.Application.Productos;

public interface IProductoRepository
{
    Task<Producto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Producto>> SearchAsync(
        string? search,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Producto>> GetAlertsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MovimientoInventario>> GetMovementsAsync(
        Guid productoId,
        TipoMovimientoInventario? tipo,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsCodeAsync(
        string codigo,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default);

    void Add(Producto producto);

    void AddMovement(MovimientoInventario movimiento);
}
