using SistLubri.Domain.Clientes;

namespace SistLubri.Application.Clientes;

public interface IClienteRepository
{
    Task<Cliente?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Cliente>> SearchAsync(
        string? search,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsDuplicateAsync(
        string nombre,
        string apellido,
        string telefono,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default);

    void Add(Cliente cliente);
}
