namespace SistLubri.Api.Contracts.Clientes;

public sealed record CrearClienteRequest(
    string Nombre,
    string Apellido,
    string Telefono,
    string? Email,
    string? Domicilio,
    string? Observaciones);

public sealed record ActualizarClienteRequest(
    string Nombre,
    string Apellido,
    string Telefono,
    string? Email,
    string? Domicilio,
    string? Observaciones);
