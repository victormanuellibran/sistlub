using Microsoft.EntityFrameworkCore;
using SistLubri.Application.Proveedores;
using SistLubri.Domain.Proveedores;

namespace SistLubri.Infrastructure.Persistence.Repositories;

internal sealed class ProveedorRepository(SistLubriDbContext dbContext) : IProveedorRepository
{
    public Task<Proveedor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Proveedores.Include(item => item.HistorialEstados)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Proveedor>> SearchAsync(
        string? search,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Proveedores.AsNoTracking().AsQueryable();
        if (!includeInactive) query = query.Where(item => item.Activo);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var normalizedDigits = new string(term.Where(char.IsDigit).ToArray());
            query = query.Where(item => item.RazonSocial.Contains(term) ||
                (normalizedDigits.Length > 0 && item.Cuit != null && item.Cuit.Contains(normalizedDigits)) ||
                (normalizedDigits.Length > 0 && item.Telefono.Contains(normalizedDigits)) ||
                (item.Email != null && item.Email.Contains(term)) ||
                (item.MarcasYProductos != null && item.MarcasYProductos.Contains(term)));
        }

        return await query.OrderBy(item => item.RazonSocial).Take(100).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsCuitAsync(
        string cuit,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default) =>
        dbContext.Proveedores.AnyAsync(item => item.Cuit == cuit &&
            (!excludingId.HasValue || item.Id != excludingId.Value), cancellationToken);

    public async Task<IReadOnlyList<Proveedor>> GetPotentialDuplicatesAsync(
        string razonSocial,
        string? telefono,
        string? email,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = razonSocial.Trim().ToLowerInvariant();
        var phoneDigits = telefono is null ? string.Empty : new string(telefono.Where(char.IsDigit).ToArray());
        var normalizedEmail = email?.Trim().ToLowerInvariant();
        var query = dbContext.Proveedores.AsNoTracking().Where(item =>
            (!excludingId.HasValue || item.Id != excludingId.Value) &&
            (item.RazonSocial.ToLower() == normalizedName ||
             (phoneDigits.Length > 0 && item.Telefono == phoneDigits) ||
             (normalizedEmail != null && item.Email == normalizedEmail)));
        return await query.OrderBy(item => item.RazonSocial).Take(20).ToListAsync(cancellationToken);
    }

    public void Add(Proveedor proveedor) => dbContext.Proveedores.Add(proveedor);
}
