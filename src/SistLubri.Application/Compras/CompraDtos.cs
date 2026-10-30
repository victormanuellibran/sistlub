using SistLubri.Domain.Compras;
using SistLubri.Domain.Productos;

namespace SistLubri.Application.Compras;

public sealed record CrearCompraCommand(Guid ProveedorId, DateOnly Fecha, string? Observaciones,
    IReadOnlyList<DetalleCompraCommand> Detalles);
public sealed record ActualizarCompraCommand(Guid ProveedorId, DateOnly Fecha, string? Observaciones,
    IReadOnlyList<DetalleCompraCommand> Detalles, string Version);
public sealed record DetalleCompraCommand(Guid ProductoId, decimal Cantidad, decimal CostoUnitarioEstimado);
public sealed record RegistrarRecepcionCompraCommand(DateOnly Fecha, string? Remito, string? Observaciones,
    IReadOnlyList<LineaRecepcionCompraCommand> Lineas);
public sealed record LineaRecepcionCompraCommand(Guid DetalleCompraId, decimal CantidadAceptada,
    decimal CantidadDanada, decimal CostoUnitario, string? Observaciones);
public sealed record RegistrarFacturaCompraCommand(string Numero, DateOnly Fecha, DateOnly? FechaVencimiento,
    decimal ImporteTotal, string? Observaciones, string Version);
public sealed record RegistrarPagoCompraCommand(DateOnly Fecha, Guid MedioPagoId, decimal Importe,
    string? Referencia, string? Observacion, string Version);

public sealed record CompraResumenDto(Guid Id, Guid ProveedorId, string NombreProveedor, DateOnly Fecha,
    EstadoRecepcionCompra EstadoRecepcion, EstadoPagoCompra EstadoPago, decimal TotalEstimado,
    decimal TotalFacturado, decimal TotalPagado, decimal SaldoPendiente, DateTimeOffset CreadaEnUtc);
public sealed record CompraDetalleDto(Guid Id, Guid ProveedorId, string NombreProveedor, DateOnly Fecha,
    string? Observaciones, EstadoRecepcionCompra EstadoRecepcion, EstadoPagoCompra EstadoPago,
    decimal TotalEstimado, decimal TotalFacturado, decimal TotalPagado, decimal SaldoPendiente,
    string Version, IReadOnlyList<DetalleCompraDto> Detalles, IReadOnlyList<RecepcionCompraDto> Recepciones,
    IReadOnlyList<FacturaCompraDto> Facturas, DateTimeOffset CreadaEnUtc, Guid CreadaPorUsuarioId);
public sealed record DetalleCompraDto(Guid Id, Guid ProductoId, string CodigoProducto, string NombreProducto,
    string UnidadMedida, decimal CantidadSolicitada, decimal CantidadAceptada, decimal CantidadPendiente,
    decimal TotalRecepcionDanada, decimal CostoUnitarioEstimado, decimal SubtotalEstimado);
public sealed record RecepcionCompraDto(Guid Id, DateOnly Fecha, string? Remito, string? Observaciones,
    Guid UsuarioId, DateTimeOffset CreadaEnUtc, IReadOnlyList<LineaRecepcionCompraDto> Lineas);
public sealed record LineaRecepcionCompraDto(Guid DetalleCompraId, decimal CantidadAceptada,
    decimal CantidadDanada, decimal CostoUnitario, string? Observaciones);
public sealed record FacturaCompraDto(Guid Id, string Numero, DateOnly Fecha, DateOnly? FechaVencimiento,
    decimal ImporteTotal, decimal TotalPagado, decimal SaldoPendiente, IReadOnlyList<PagoCompraDto> Pagos);
public sealed record PagoCompraDto(Guid Id, DateOnly Fecha, Guid MedioPagoId, decimal Importe,
    string? Referencia, string? Observacion, Guid UsuarioId);
