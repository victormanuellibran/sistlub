using SistLubri.Domain.Productos;

namespace SistLubri.Application.Productos;

public sealed record ProductoResumenDto(
    Guid Id,
    string Codigo,
    string Nombre,
    string? Marca,
    string? Categoria,
    string Presentacion,
    UnidadMedidaProducto UnidadMedida,
    decimal PrecioVenta,
    decimal StockActual,
    decimal StockMinimo,
    EstadoInventario EstadoInventario,
    bool EsFraccionado,
    bool Activo);

public sealed record ProductoDetalleDto(
    Guid Id,
    string Codigo,
    string Nombre,
    string? Marca,
    string? Categoria,
    string Presentacion,
    UnidadMedidaProducto UnidadMedida,
    decimal PrecioVenta,
    decimal CostoCompra,
    decimal StockActual,
    decimal StockMinimo,
    EstadoInventario EstadoInventario,
    bool EsFraccionado,
    bool Activo,
    DateTimeOffset CreadoEnUtc,
    DateTimeOffset? ActualizadoEnUtc);

public sealed record MovimientoInventarioDto(
    Guid Id,
    Guid ProductoId,
    TipoMovimientoInventario Tipo,
    decimal Cantidad,
    decimal StockAnterior,
    decimal StockResultante,
    string Motivo,
    Guid? ReferenciaId,
    DateTimeOffset FechaUtc);

public sealed record CrearProductoCommand(
    string Codigo,
    string Nombre,
    string? Marca,
    string? Categoria,
    string Presentacion,
    UnidadMedidaProducto UnidadMedida,
    decimal PrecioVenta,
    decimal CostoCompra,
    bool EsFraccionado,
    decimal StockInicial,
    decimal StockMinimo);

public sealed record ActualizarProductoCommand(
    string Codigo,
    string Nombre,
    string? Marca,
    string? Categoria,
    string Presentacion,
    UnidadMedidaProducto UnidadMedida,
    decimal PrecioVenta,
    decimal CostoCompra,
    bool EsFraccionado);

public sealed record RegistrarMovimientoInventarioCommand(
    TipoMovimientoInventario Tipo,
    decimal Cantidad,
    string Motivo,
    Guid? ReferenciaId);
