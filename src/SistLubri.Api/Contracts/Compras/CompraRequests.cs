using System.ComponentModel.DataAnnotations;

namespace SistLubri.Api.Contracts.Compras;

public sealed record DetalleCompraRequest(Guid ProductoId,
    [Range(0.001, 1000000)] decimal Cantidad,
    [Range(0, 100000000)] decimal CostoUnitarioEstimado);

public sealed record CrearCompraRequest(Guid ProveedorId, DateOnly Fecha, [StringLength(1000)] string? Observaciones,
    [Required, MinLength(1), MaxLength(100)] IReadOnlyList<DetalleCompraRequest> Detalles);

public sealed record ActualizarCompraRequest(Guid ProveedorId, DateOnly Fecha, [StringLength(1000)] string? Observaciones,
    [Required, MinLength(1), MaxLength(100)] IReadOnlyList<DetalleCompraRequest> Detalles, [Required] string Version);

public sealed record LineaRecepcionCompraRequest(Guid DetalleCompraId,
    [Range(0, 1000000)] decimal CantidadAceptada,
    [Range(0, 1000000)] decimal CantidadDanada,
    [Range(0, 100000000)] decimal CostoUnitario,
    [StringLength(500)] string? Observaciones);

public sealed record RegistrarRecepcionCompraRequest(DateOnly Fecha, [StringLength(120)] string? Remito,
    [StringLength(1000)] string? Observaciones,
    [Required, MinLength(1), MaxLength(100)] IReadOnlyList<LineaRecepcionCompraRequest> Lineas);

public sealed record RegistrarFacturaCompraRequest([Required, StringLength(120, MinimumLength = 1)] string Numero,
    DateOnly Fecha, DateOnly? FechaVencimiento,
    [Range(0.01, 100000000)] decimal ImporteTotal, [StringLength(1000)] string? Observaciones,
    [Required] string Version);

public sealed record RegistrarPagoCompraRequest(DateOnly Fecha, Guid MedioPagoId,
    [Range(0.01, 100000000)] decimal Importe,
    [StringLength(120)] string? Referencia, [StringLength(500)] string? Observacion, [Required] string Version);

public sealed record CerrarRecepcionCompraRequest([Required, StringLength(500, MinimumLength = 3)] string Motivo,
    [Required] string Version);

public sealed record AnularCompraRequest([Required, StringLength(500, MinimumLength = 3)] string Motivo,
    [Required] string Version);
