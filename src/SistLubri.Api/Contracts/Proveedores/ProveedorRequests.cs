using System.ComponentModel.DataAnnotations;

namespace SistLubri.Api.Contracts.Proveedores;

public sealed record CrearProveedorRequest(
    [Required, StringLength(160, MinimumLength = 2)] string RazonSocial,
    [Required, StringLength(30)] string Telefono,
    string? Cuit = null,
    [StringLength(120)] string? ContactoNombre = null,
    [EmailAddress, StringLength(254)] string? Email = null,
    [StringLength(250)] string? Direccion = null,
    [StringLength(500)] string? MarcasYProductos = null,
    [StringLength(120)] string? CondicionPagoHabitual = null,
    [Range(0, 3650)] int? PlazoPagoHabitualDias = null,
    [StringLength(80)] string? PeriodoPagoHabitual = null,
    [StringLength(1000)] string? Observaciones = null,
    bool ConfirmarPosiblesDuplicados = false);

public sealed record ActualizarProveedorRequest(
    [Required, StringLength(160, MinimumLength = 2)] string RazonSocial,
    [Required, StringLength(30)] string Telefono,
    string? Cuit = null,
    [StringLength(120)] string? ContactoNombre = null,
    [EmailAddress, StringLength(254)] string? Email = null,
    [StringLength(250)] string? Direccion = null,
    [StringLength(500)] string? MarcasYProductos = null,
    [StringLength(120)] string? CondicionPagoHabitual = null,
    [Range(0, 3650)] int? PlazoPagoHabitualDias = null,
    [StringLength(80)] string? PeriodoPagoHabitual = null,
    [StringLength(1000)] string? Observaciones = null,
    bool ConfirmarPosiblesDuplicados = false);

public sealed record DesactivarProveedorRequest(
    [Required, StringLength(500, MinimumLength = 3)] string Motivo);
