using SistLubri.Domain.Vehiculos;

namespace SistLubri.Application.Vehiculos;

public interface IVehiculoRepository
{
    Task<Vehiculo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Vehiculo?> GetByPatenteAsync(string patente, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Vehiculo>> SearchAsync(string? search, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Vehiculo>> GetByClienteIdAsync(
        Guid clienteId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsPatenteAsync(
        string patente,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default);

    void Add(Vehiculo vehiculo);
}
