using Microsoft.EntityFrameworkCore;
using SistLubri.Application.Vehiculos;
using SistLubri.Domain.Vehiculos;

namespace SistLubri.Infrastructure.Persistence.Repositories;

internal sealed class VehiculoRepository(SistLubriDbContext dbContext) : IVehiculoRepository
{
    public Task<Vehiculo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Vehiculos
            .Include(vehiculo => vehiculo.Cliente)
            .SingleOrDefaultAsync(vehiculo => vehiculo.Id == id, cancellationToken);
    }

    public Task<Vehiculo?> GetByPatenteAsync(string patente, CancellationToken cancellationToken = default)
    {
        return dbContext.Vehiculos
            .AsNoTracking()
            .Include(vehiculo => vehiculo.Cliente)
            .SingleOrDefaultAsync(vehiculo => vehiculo.Patente == patente, cancellationToken);
    }

    public async Task<IReadOnlyList<Vehiculo>> SearchAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Vehiculos
            .AsNoTracking()
            .Include(vehiculo => vehiculo.Cliente)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var patente = new string(term.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            query = query.Where(vehiculo =>
                vehiculo.Patente.Contains(patente) ||
                vehiculo.Marca.Contains(term) ||
                vehiculo.Modelo.Contains(term) ||
                vehiculo.Cliente.Nombre.Contains(term) ||
                vehiculo.Cliente.Apellido.Contains(term));
        }

        return await query
            .OrderBy(vehiculo => vehiculo.Patente)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Vehiculo>> GetByClienteIdAsync(
        Guid clienteId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Vehiculos
            .AsNoTracking()
            .Where(vehiculo => vehiculo.ClienteId == clienteId)
            .OrderBy(vehiculo => vehiculo.Patente)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsPatenteAsync(
        string patente,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Vehiculos.AnyAsync(
            vehiculo =>
                vehiculo.Patente == patente &&
                (!excludingId.HasValue || vehiculo.Id != excludingId.Value),
            cancellationToken);
    }

    public void Add(Vehiculo vehiculo)
    {
        dbContext.Vehiculos.Add(vehiculo);
    }
}
