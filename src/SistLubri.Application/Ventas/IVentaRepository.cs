using SistLubri.Domain.Ventas;

namespace SistLubri.Application.Ventas;

public interface IVentaRepository
{
    Task<Venta?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Venta?> GetByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Venta>> SearchAsync(DateTimeOffset? desde, DateTimeOffset? hasta, Guid? clienteId, EstadoVenta? estado, CancellationToken cancellationToken = default);
    void Add(Venta venta);
}
