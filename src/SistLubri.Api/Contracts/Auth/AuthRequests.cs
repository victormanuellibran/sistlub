using System.ComponentModel.DataAnnotations;

namespace SistLubri.Api.Contracts.Auth;

public sealed record InicializarAdministradorRequest(
    [Required] string ClaveInicial,
    [Required, StringLength(120, MinimumLength = 2)] string NombreCompleto,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(128, MinimumLength = 12)] string Contrasena);

public sealed record IniciarSesionRequest(
    [Required, EmailAddress] string Email,
    [Required] string Contrasena);

public sealed record CrearUsuarioRequest(
    [Required, StringLength(120, MinimumLength = 2)] string NombreCompleto,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(128, MinimumLength = 12)] string Contrasena,
    [Required] string Rol);

public sealed record CambiarEstadoUsuarioRequest(bool Activo);

public sealed record CambiarRolUsuarioRequest([Required] string Rol);

public sealed record SesionResponse(
    string Token,
    DateTimeOffset ExpiraEnUtc,
    Guid UsuarioId,
    string NombreCompleto,
    string Email,
    IReadOnlyList<string> Roles);

public sealed record UsuarioResponse(
    Guid Id,
    string NombreCompleto,
    string Email,
    bool Activo,
    IReadOnlyList<string> Roles);
