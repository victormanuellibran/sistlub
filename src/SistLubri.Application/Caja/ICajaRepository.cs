using SistLubri.Domain.Caja;

namespace SistLubri.Application.Caja;

public interface ICajaRepository
{
    Task<JornadaCaja?> GetActualAsync(CancellationToken cancellationToken = default);
    Task<JornadaCaja?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<JornadaCaja?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<JornadaCaja?> GetMostRecentBeforeAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<ConfiguracionCaja?> GetConfigurationAsync(CancellationToken cancellationToken = default);
    Task<bool> HasOpenAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsOnDateAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<bool> ExistsAnyAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JornadaCaja>> SearchAsync(DateOnly? desde, DateOnly? hasta, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MedioPago>> GetMediosPagoAsync(CancellationToken cancellationToken = default);
    Task<MedioPago?> GetMedioPagoAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsPaymentMethodNameAsync(string nombre, Guid? excludingId = null, CancellationToken cancellationToken = default);
    void Add(JornadaCaja jornada);
    void Add(MovimientoCaja movimiento);
    void Add(MedioPago medioPago);
    void Add(ConfiguracionCaja configuration);
    void Add(RegistroCierreCaja cierre);
    void Add(RegistroAperturaCaja apertura);
}
