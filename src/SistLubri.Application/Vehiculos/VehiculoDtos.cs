namespace SistLubri.Application.Vehiculos;

public sealed record VehiculoResumenDto(
    Guid Id,
    string Patente,
    string Marca,
    string Modelo,
    int? Anio,
    int? KilometrajeActual,
    int? ProximoCambioKilometraje);

public sealed record VehiculoDetalleDto(
    Guid Id,
    Guid ClienteId,
    string Cliente,
    string Patente,
    string Marca,
    string Modelo,
    int? Anio,
    int? KilometrajeActual,
    int? ProximoCambioKilometraje,
    string? Observaciones,
    DateTimeOffset CreadoEnUtc,
    DateTimeOffset? ActualizadoEnUtc);

public sealed record CrearVehiculoCommand(
    string Patente,
    string Marca,
    string Modelo,
    int? Anio,
    int? KilometrajeActual,
    int? ProximoCambioKilometraje,
    string? Observaciones);

public sealed record ActualizarVehiculoCommand(
    string Marca,
    string Modelo,
    int? Anio,
    int? KilometrajeActual,
    int? ProximoCambioKilometraje,
    string? Observaciones);
