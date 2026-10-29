using System.ComponentModel.DataAnnotations;

namespace SistLubri.Api.Contracts.Servicios;

public sealed record CrearDetalleServicioRequest(Guid ProductoId, decimal Cantidad);

public sealed record ActualizarDetallesServicioRequest(
    IReadOnlyList<CrearDetalleServicioRequest> Detalles,
    string? TrabajoSolicitado = null,
    string? Observaciones = null,
    decimal? ManoObra = null);

public sealed record CrearServicioVehicularRequest(
    string Patente,
    int KilometrajeIngreso,
    string TrabajoSolicitado,
    string? InspeccionPrevia,
    string? DanosExistentes,
    string? Observaciones,
    decimal ManoObra,
    IReadOnlyList<CrearDetalleServicioRequest> Detalles);

public sealed record ConfirmarConsumoServicioRequest(Guid ProductoId, decimal CantidadUtilizada);

public sealed record FinalizarServicioRequest(
    int KilometrajeFinal,
    int? ProximoCambioKilometraje,
    [Required] IReadOnlyList<ConfirmarConsumoServicioRequest> ProductosUtilizados);

public sealed record AnularServicioRequest(
    [Required, StringLength(500, MinimumLength = 3)] string Motivo,
    [Required] IReadOnlyList<ConfirmarConsumoServicioRequest> ProductosUtilizados);
