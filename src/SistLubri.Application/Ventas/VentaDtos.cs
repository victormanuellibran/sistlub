using SistLubri.Domain.Ventas;

namespace SistLubri.Application.Ventas;

public sealed record DetalleVentaCommand(Guid ProductoId, decimal Cantidad, decimal? PrecioUnitario = null, decimal Descuento = 0);
public sealed record PagoVentaCommand(Guid MedioPagoId, decimal Importe, string? Referencia = null, string? Observacion = null);
public sealed record CrearVentaCommand(Guid? ClienteId, string? ReferenciaFactura, decimal DescuentoGeneral, IReadOnlyList<DetalleVentaCommand> Detalles, IReadOnlyList<PagoVentaCommand> Pagos);
public sealed record CobrarServicioCommand(decimal ImporteManoObra, decimal DescuentoGeneral, IReadOnlyList<PagoVentaCommand> Pagos, string? ReferenciaFactura);
public sealed record EditarVentaPendienteCommand(decimal? ImporteManoObra, decimal DescuentoGeneral, string? ReferenciaFactura, IReadOnlyList<DetalleVentaCommand> Detalles);
public sealed record DetalleDevolucionCommand(Guid ProductoId, decimal Cantidad, bool ReingresarAlInventario);
public sealed record DevolverVentaCommand(string Motivo, IReadOnlyList<PagoVentaCommand> Reintegros, IReadOnlyList<DetalleDevolucionCommand> Detalles);
public sealed record VentaDetalleDto(Guid Id, OrigenVenta Origen, EstadoVenta Estado, Guid? ClienteId, string? ClienteNombre, string? Patente, Guid? ServicioId, decimal ImporteManoObra, decimal TotalBruto, decimal DescuentoTotal, decimal Total, bool InventarioDescontado, string? ReferenciaFactura, string? MotivoAnulacion, string? MotivoDevolucion, DateTimeOffset CreadaEnUtc, Guid CreadaPorUsuarioId, DateTimeOffset ActualizadaEnUtc, Guid ActualizadaPorUsuarioId, IReadOnlyList<DetalleVentaDto> Detalles, IReadOnlyList<PagoVentaDto> Pagos, IReadOnlyList<PagoVentaDto> Reintegros);
public sealed record DetalleVentaDto(Guid ProductoId, string Codigo, string Nombre, string Presentacion, decimal Cantidad, decimal PrecioUnitario, decimal Descuento, decimal Subtotal);
public sealed record PagoVentaDto(Guid Id, Guid MedioPagoId, string NombreMedioPago, decimal Importe, string? Referencia, string? Observacion, Guid UsuarioId, DateTimeOffset FechaUtc, Guid? MovimientoCajaId);
