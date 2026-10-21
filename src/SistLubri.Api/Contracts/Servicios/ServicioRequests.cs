namespace SistLubri.Api.Contracts.Servicios;

public sealed record CrearDetalleServicioRequest(Guid ProductoId, decimal Cantidad);

public sealed record ActualizarDetallesServicioRequest(IReadOnlyList<CrearDetalleServicioRequest> Detalles);

public sealed record CrearServicioVehicularRequest(
    string Patente,
    int KilometrajeIngreso,
    string TrabajoSolicitado,
    string? InspeccionPrevia,
    string? DanosExistentes,
    string? Observaciones,
    decimal ManoObra,
    IReadOnlyList<CrearDetalleServicioRequest> Detalles);

public sealed record FinalizarServicioRequest(int KilometrajeFinal, int? ProximoCambioKilometraje);

public sealed record AnularServicioRequest(string Motivo);
