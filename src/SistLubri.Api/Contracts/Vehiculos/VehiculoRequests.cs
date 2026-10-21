namespace SistLubri.Api.Contracts.Vehiculos;

public sealed record CrearVehiculoRequest(
    string Patente,
    string Marca,
    string Modelo,
    int? Anio,
    int? KilometrajeActual,
    int? ProximoCambioKilometraje,
    string? Observaciones);

public sealed record ActualizarVehiculoRequest(
    string Marca,
    string Modelo,
    int? Anio,
    int? KilometrajeActual,
    int? ProximoCambioKilometraje,
    string? Observaciones);

public sealed record CambiarTitularRequest(Guid ClienteId);
