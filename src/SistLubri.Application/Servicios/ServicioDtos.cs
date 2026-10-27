using SistLubri.Domain.Servicios;
using SistLubri.Domain.Productos;

namespace SistLubri.Application.Servicios;

public sealed record DetalleServicioDto(
    Guid Id,
    Guid ProductoId,
    string CodigoProducto,
    string NombreProducto,
    UnidadMedidaProducto UnidadMedida,
    bool EsFraccionado,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal CostoUnitario,
    decimal Subtotal);

public sealed record ServicioVehicularDto(
    Guid Id,
    Guid VehiculoId,
    Guid ClienteId,
    string Patente,
    string ClienteNombre,
    int KilometrajeIngreso,
    string TrabajoSolicitado,
    string? InspeccionPrevia,
    string? DanosExistentes,
    string? Observaciones,
    decimal ManoObra,
    decimal TotalProductos,
    decimal Total,
    EstadoServicio Estado,
    DateTimeOffset CreadoEnUtc,
    DateTimeOffset? FinalizadoEnUtc,
    DateTimeOffset? AnuladoEnUtc,
    string? MotivoAnulacion,
    IReadOnlyList<DetalleServicioDto> Detalles);

public sealed record CrearDetalleServicioCommand(Guid ProductoId, decimal Cantidad);

public sealed record CrearServicioVehicularCommand(
    string Patente,
    int KilometrajeIngreso,
    string TrabajoSolicitado,
    string? InspeccionPrevia,
    string? DanosExistentes,
    string? Observaciones,
    decimal ManoObra,
    IReadOnlyList<CrearDetalleServicioCommand> Detalles);
