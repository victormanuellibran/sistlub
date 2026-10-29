using SistLubri.Domain.Gastos;

namespace SistLubri.Application.Gastos;

public sealed record FiltroGastos(
    DateOnly? Desde,
    DateOnly? Hasta,
    DateOnly? VencimientoDesde,
    DateOnly? VencimientoHasta,
    CategoriaGasto? Categoria,
    EstadoGasto? Estado,
    SituacionGasto? Situacion,
    Guid? MedioPagoId,
    Guid? ProveedorId,
    string? Entidad,
    decimal? ImporteMinimo,
    decimal? ImporteMaximo,
    Guid? UsuarioId,
    string? Buscar);

public interface IGastoRepository
{
    Task<Gasto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Gasto>> SearchAsync(FiltroGastos filtro, DateOnly hoy, CancellationToken cancellationToken = default);
    void Add(Gasto gasto);
    void Add(PagoGasto pago);
    void Add(ReintegroGasto reintegro);
}
