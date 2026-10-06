using SistLubri.Application.Vehiculos;

namespace SistLubri.Application.Clientes;

public sealed record ClienteResumenDto(
    Guid Id,
    string Nombre,
    string Apellido,
    string Telefono,
    string? Email,
    bool Activo);

public sealed record ClienteDetalleDto(
    Guid Id,
    string Nombre,
    string Apellido,
    string Telefono,
    string? Email,
    string? Domicilio,
    string? Observaciones,
    bool Activo,
    DateTimeOffset CreadoEnUtc,
    DateTimeOffset? ActualizadoEnUtc,
    DateTimeOffset? DesactivadoEnUtc,
    IReadOnlyList<VehiculoResumenDto> Vehiculos);

public sealed record CrearClienteCommand(
    string Nombre,
    string Apellido,
    string Telefono,
    string? Email,
    string? Domicilio,
    string? Observaciones);

public sealed record ActualizarClienteCommand(
    string Nombre,
    string Apellido,
    string Telefono,
    string? Email,
    string? Domicilio,
    string? Observaciones);
