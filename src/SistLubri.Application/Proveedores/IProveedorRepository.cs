using SistLubri.Domain.Proveedores;

namespace SistLubri.Application.Proveedores;

public interface IProveedorRepository
{
    Task<Proveedor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Proveedor>> SearchAsync(string? search, bool includeInactive, CancellationToken cancellationToken = default);
    Task<bool> ExistsCuitAsync(string cuit, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Proveedor>> GetPotentialDuplicatesAsync(
        string razonSocial,
        string? telefono,
        string? email,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default);
    void Add(Proveedor proveedor);
}
