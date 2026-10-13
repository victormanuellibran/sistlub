using SistLubri.Domain.Productos;

namespace SistLubri.Api.Contracts.Productos;

public sealed record CrearProductoRequest(
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

public sealed record ActualizarProductoRequest(
    string Codigo,
    string Nombre,
    string? Marca,
    string? Categoria,
    string Presentacion,
    UnidadMedidaProducto UnidadMedida,
    decimal PrecioVenta,
    decimal CostoCompra,
    bool EsFraccionado);

public sealed record CambiarEstadoProductoRequest(bool Activo);

public sealed record ConfigurarStockMinimoRequest(decimal StockMinimo);

public sealed record RegistrarMovimientoInventarioRequest(
    TipoMovimientoInventario Tipo,
    decimal Cantidad,
    string Motivo,
    Guid? ReferenciaId);
