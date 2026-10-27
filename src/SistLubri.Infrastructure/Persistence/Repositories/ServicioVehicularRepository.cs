using Microsoft.EntityFrameworkCore;
using SistLubri.Application.Servicios;
using SistLubri.Domain.Servicios;

namespace SistLubri.Infrastructure.Persistence.Repositories;

internal sealed class ServicioVehicularRepository(SistLubriDbContext dbContext) : IServicioVehicularRepository
{
    public Task<ServicioVehicular?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.ServiciosVehiculares
            .Include(servicio => servicio.Detalles)
            .SingleOrDefaultAsync(servicio => servicio.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ServicioVehicular>> GetByVehiculoIdAsync(
        Guid vehiculoId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ServiciosVehiculares
            .AsNoTracking()
            .Include(servicio => servicio.Detalles)
            .Where(servicio => servicio.VehiculoId == vehiculoId)
            .OrderByDescending(servicio => servicio.CreadoEnUtc)
            .Take(200)
            .ToListAsync(cancellationToken);
    }

    public void Add(ServicioVehicular servicio)
    {
        dbContext.ServiciosVehiculares.Add(servicio);
    }
}
