using Microsoft.EntityFrameworkCore;
using SistLubri.Application.Clientes;
using SistLubri.Domain.Clientes;

namespace SistLubri.Infrastructure.Persistence.Repositories;

internal sealed class ClienteRepository(SistLubriDbContext dbContext) : IClienteRepository
{
    public Task<Cliente?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Clientes.SingleOrDefaultAsync(cliente => cliente.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Cliente>> SearchAsync(
        string? search,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Clientes.AsNoTracking().AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(cliente => cliente.Activo);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var phone = new string(term.Where(char.IsDigit).ToArray());
            query = query.Where(cliente =>
                cliente.Nombre.Contains(term) ||
                cliente.Apellido.Contains(term) ||
                (cliente.Nombre + " " + cliente.Apellido).Contains(term) ||
                (phone.Length > 0 && cliente.Telefono.Contains(phone)));
        }

        return await query
            .OrderBy(cliente => cliente.Apellido)
            .ThenBy(cliente => cliente.Nombre)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsDuplicateAsync(
        string nombre,
        string apellido,
        string telefono,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Clientes.AnyAsync(
            cliente =>
                cliente.Nombre == nombre &&
                cliente.Apellido == apellido &&
                cliente.Telefono == telefono &&
                (!excludingId.HasValue || cliente.Id != excludingId.Value),
            cancellationToken);
    }

    public void Add(Cliente cliente)
    {
        dbContext.Clientes.Add(cliente);
    }
}
