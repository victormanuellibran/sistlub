using System.ComponentModel.DataAnnotations;

namespace SistLubri.Api.Contracts.Ventas;

public sealed record DetalleVentaRequest(Guid ProductoId, [Range(0.001, 1000000)] decimal Cantidad, [Range(0, 100000000)] decimal? PrecioUnitario, [Range(0, 100000000)] decimal Descuento);
public sealed record PagoVentaRequest(Guid MedioPagoId, [Range(0.01, 100000000)] decimal Importe, string? Referencia, string? Observacion);
public sealed record CrearVentaRequest(Guid? ClienteId, string? ReferenciaFactura, [Range(0, 100000000)] decimal DescuentoGeneral, [Required, MinLength(1)] IReadOnlyList<DetalleVentaRequest> Detalles, [Required, MinLength(1)] IReadOnlyList<PagoVentaRequest> Pagos);
public sealed record CobrarServicioVentaRequest([Range(0, 100000000)] decimal ImporteManoObra, [Range(0, 100000000)] decimal DescuentoGeneral, string? ReferenciaFactura, [Required, MinLength(1)] IReadOnlyList<PagoVentaRequest> Pagos);
public sealed record DetalleDevolucionRequest(Guid ProductoId, decimal Cantidad, bool ReingresarAlInventario);
public sealed record DevolverVentaRequest([Required, StringLength(500, MinimumLength = 3)] string Motivo, [Required, MinLength(1)] IReadOnlyList<PagoVentaRequest> Reintegros, IReadOnlyList<DetalleDevolucionRequest> Detalles);
public sealed record AnularVentaRequest([Required, StringLength(500, MinimumLength = 3)] string Motivo);
public sealed record EditarVentaRequest([Range(0, 100000000)] decimal? ImporteManoObra, [Range(0, 100000000)] decimal DescuentoGeneral, string? ReferenciaFactura, [Required, MinLength(1)] IReadOnlyList<DetalleVentaRequest> Detalles);
