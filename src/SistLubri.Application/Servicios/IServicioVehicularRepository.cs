using SistLubri.Domain.Servicios;

namespace SistLubri.Application.Servicios;

public interface IServicioVehicularRepository
{
    Task<ServicioVehicular?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServicioVehicular>> GetByVehiculoIdAsync(
        Guid vehiculoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServicioVehicular>> GetInProgressAsync(CancellationToken cancellationToken = default);

    void Add(ServicioVehicular servicio);
}
