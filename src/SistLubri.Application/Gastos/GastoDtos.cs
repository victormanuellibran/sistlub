using SistLubri.Domain.Gastos;

namespace SistLubri.Application.Gastos;

public sealed record PagoGastoCommand(Guid MedioPagoId, decimal Importe, string? Referencia, string? Observacion);
public sealed record CrearGastoCommand(
    CategoriaGasto Categoria,
    string? CategoriaDetalle,
    string Descripcion,
    DateOnly Fecha,
    decimal ImporteTotal,
    string? EntidadRelacionada,
    Guid? ProveedorId,
    DateOnly? FechaVencimiento,
    bool VencimientoEstimado,
    string? ReferenciaComprobante,
    string? Observaciones,
    DateOnly? FechaPagoInicial,
    IReadOnlyList<PagoGastoCommand> PagosIniciales,
    bool EsCompraMercaderia);
public sealed record ActualizarGastoCommand(
    CategoriaGasto Categoria,
    string? CategoriaDetalle,
    string Descripcion,
    DateOnly Fecha,
    decimal ImporteTotal,
    string? EntidadRelacionada,
    Guid? ProveedorId,
    DateOnly? FechaVencimiento,
    bool VencimientoEstimado,
    string? ReferenciaComprobante,
    string? Observaciones,
    string Version);
public sealed record RegistrarPagosGastoCommand(DateOnly Fecha, IReadOnlyList<PagoGastoCommand> Pagos);
public sealed record AnularGastoCommand(
    string Motivo,
    DateOnly? FechaReintegro,
    IReadOnlyList<PagoGastoCommand> Reintegros);

public sealed record PagoGastoDto(
    Guid Id,
    Guid MedioPagoId,
    string NombreMedioPago,
    decimal Importe,
    DateOnly Fecha,
    string? Referencia,
    string? Observacion,
    Guid UsuarioId,
    DateTimeOffset CreadoEnUtc,
    Guid? MovimientoCajaId);
public sealed record ReintegroGastoDto(
    Guid Id,
    Guid MedioPagoId,
    string NombreMedioPago,
    decimal Importe,
    DateOnly Fecha,
    string? Referencia,
    string? Observacion,
    Guid UsuarioId,
    DateTimeOffset CreadoEnUtc,
    Guid? MovimientoCajaId);
public sealed record GastoResumenDto(
    Guid Id,
    CategoriaGasto Categoria,
    string? CategoriaDetalle,
    string Descripcion,
    DateOnly Fecha,
    decimal ImporteTotal,
    decimal TotalPagado,
    decimal SaldoPendiente,
    EstadoGasto Estado,
    SituacionGasto Situacion,
    DateOnly? FechaVencimiento,
    bool VencimientoEstimado,
    string? EntidadRelacionada,
    Guid? ProveedorId,
    string? NombreProveedor,
    DateTimeOffset CreadoEnUtc,
    Guid CreadoPorUsuarioId);
public sealed record GastoDetalleDto(
    Guid Id,
    CategoriaGasto Categoria,
    string? CategoriaDetalle,
    string Descripcion,
    DateOnly Fecha,
    decimal ImporteTotal,
    decimal TotalPagado,
    decimal TotalReintegrado,
    decimal SaldoPendiente,
    EstadoGasto Estado,
    SituacionGasto Situacion,
    DateOnly? FechaVencimiento,
    bool VencimientoEstimado,
    string? EntidadRelacionada,
    Guid? ProveedorId,
    string? NombreProveedor,
    string? ReferenciaComprobante,
    string? Observaciones,
    DateTimeOffset CreadoEnUtc,
    Guid CreadoPorUsuarioId,
    DateTimeOffset ActualizadoEnUtc,
    Guid ActualizadoPorUsuarioId,
    DateTimeOffset? AnuladoEnUtc,
    Guid? AnuladoPorUsuarioId,
    string? MotivoAnulacion,
    string Version,
    IReadOnlyList<PagoGastoDto> Pagos,
    IReadOnlyList<ReintegroGastoDto> Reintegros);
