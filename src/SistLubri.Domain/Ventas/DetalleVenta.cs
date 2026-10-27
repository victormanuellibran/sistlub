using SistLubri.Domain.Common;
using SistLubri.Domain.Productos;

namespace SistLubri.Domain.Ventas;

public sealed class DetalleVenta : Entity
{
    private DetalleVenta()
    {
    }

    internal DetalleVenta(
        Guid ventaId,
        Guid productoId,
        string codigoProducto,
        string nombreProducto,
        string presentacion,
        UnidadMedidaProducto unidadMedida,
        bool esFraccionado,
        decimal cantidad,
        decimal precioUnitario,
        decimal descuento,
        bool esDetalleTecnico)
    {
        if (productoId == Guid.Empty)
        {
            throw new DomainException("El detalle debe referenciar un producto valido.");
        }

        VentaId = ventaId;
        ProductoId = productoId;
        CodigoProducto = Normalizar(codigoProducto, 40, "El codigo");
        NombreProducto = Normalizar(nombreProducto, 120, "El nombre");
        Presentacion = Normalizar(presentacion, 80, "La presentacion");
        UnidadMedida = unidadMedida;
        EsFraccionado = esFraccionado;
        EsDetalleTecnico = esDetalleTecnico;
        ActualizarImportes(cantidad, precioUnitario, descuento);
    }

    public Guid VentaId { get; private init; }

    public Venta Venta { get; private init; } = null!;

    public Guid ProductoId { get; private init; }

    public string CodigoProducto { get; private set; } = string.Empty;

    public string NombreProducto { get; private set; } = string.Empty;

    public string Presentacion { get; private set; } = string.Empty;

    public UnidadMedidaProducto UnidadMedida { get; private set; }

    public bool EsFraccionado { get; private set; }

    public bool EsDetalleTecnico { get; private init; }

    public decimal Cantidad { get; private set; }

    public decimal PrecioUnitario { get; private set; }

    public decimal Descuento { get; private set; }
    public decimal CantidadDevuelta { get; private set; }

    public decimal SubtotalBruto => decimal.Round(Cantidad * PrecioUnitario, 2);

    public decimal Subtotal => decimal.Round(SubtotalBruto - Descuento, 2);

    public void RegistrarDevolucion(decimal cantidad)
    {
        if (cantidad <= 0 || decimal.Round(CantidadDevuelta + cantidad, 3) > Cantidad)
            throw new DomainException("La cantidad devuelta supera la cantidad vendida disponible.");
        CantidadDevuelta = decimal.Round(CantidadDevuelta + cantidad, 3);
    }

    internal void ActualizarCantidadYPrecios(decimal cantidad, decimal precioUnitario, decimal descuento)
    {
        if (EsDetalleTecnico)
        {
            throw new DomainException("La cantidad de un producto utilizado en un servicio no se puede cambiar desde la venta.");
        }

        ActualizarImportes(cantidad, precioUnitario, descuento);
    }

    internal void ActualizarImportesComerciales(decimal precioUnitario, decimal descuento)
    {
        ActualizarImportes(Cantidad, precioUnitario, descuento);
    }

    private void ActualizarImportes(decimal cantidad, decimal precioUnitario, decimal descuento)
    {
        var normalizedQuantity = decimal.Round(cantidad, 3);
        var normalizedPrice = decimal.Round(precioUnitario, 2);
        var normalizedDiscount = decimal.Round(descuento, 2);

        if (normalizedQuantity <= 0)
        {
            throw new DomainException("La cantidad debe ser mayor que cero.");
        }

        if (normalizedPrice < 0 || normalizedDiscount < 0)
        {
            throw new DomainException("El precio y el descuento no pueden ser negativos.");
        }

        if (normalizedDiscount > decimal.Round(normalizedQuantity * normalizedPrice, 2))
        {
            throw new DomainException("El descuento no puede superar el importe bruto del producto.");
        }

        Cantidad = normalizedQuantity;
        PrecioUnitario = normalizedPrice;
        Descuento = normalizedDiscount;
    }

    private static string Normalizar(string? value, int maxLength, string field)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maxLength)
        {
            throw new DomainException($"{field} es obligatorio y no puede superar {maxLength} caracteres.");
        }

        return normalized;
    }
}
