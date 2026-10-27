using SistLubri.Domain.Productos;

namespace SistLubri.Domain.Ventas;

public sealed record DetalleVentaInicial(
    Guid ProductoId,
    string CodigoProducto,
    string NombreProducto,
    string Presentacion,
    UnidadMedidaProducto UnidadMedida,
    bool EsFraccionado,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal Descuento = 0);
