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
    DateTimeOffset? ActualizadoEnUtc,
    Guid? ActualizadoPorUsuarioId,
    DateTimeOffset? FinalizadoEnUtc,
    DateTimeOffset? AnuladoEnUtc,
    string? MotivoAnulacion,
    IReadOnlyList<DetalleServicioDto> Detalles);

public sealed record CrearDetalleServicioCommand(Guid ProductoId, decimal Cantidad);

public sealed record ConfirmarConsumoServicioCommand(Guid ProductoId, decimal CantidadUtilizada);

public sealed record ActualizarServicioVehicularCommand(
    IReadOnlyList<CrearDetalleServicioCommand> Detalles,
    string? TrabajoSolicitado = null,
    string? Observaciones = null,
    decimal? ManoObra = null);

public sealed record CerrarServicioVehicularCommand(
    IReadOnlyList<ConfirmarConsumoServicioCommand> ProductosUtilizados,
    int? KilometrajeFinal = null,
    int? ProximoCambioKilometraje = null,
    string? MotivoAnulacion = null);

public sealed record CrearServicioVehicularCommand(
    string Patente,
    int KilometrajeIngreso,
    string TrabajoSolicitado,
    string? InspeccionPrevia,
    string? DanosExistentes,
    string? Observaciones,
    decimal ManoObra,
    IReadOnlyList<CrearDetalleServicioCommand> Detalles);
