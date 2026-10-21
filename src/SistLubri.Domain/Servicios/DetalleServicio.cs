using SistLubri.Domain.Common;
using SistLubri.Domain.Productos;

namespace SistLubri.Domain.Servicios;

public sealed class DetalleServicio : Entity
{
    private DetalleServicio()
    {
    }

    internal DetalleServicio(
        Guid servicioVehicularId,
        Guid productoId,
        string codigoProducto,
        string nombreProducto,
        UnidadMedidaProducto unidadMedida,
        bool esFraccionado,
        decimal cantidad,
        decimal precioUnitario,
        decimal costoUnitario)
    {
        if (productoId == Guid.Empty)
        {
            throw new DomainException("Cada detalle debe referenciar un producto valido.");
        }

        ServicioVehicularId = servicioVehicularId;
        ProductoId = productoId;
        CodigoProducto = Normalizar(codigoProducto, 40, "El codigo del producto");
        NombreProducto = Normalizar(nombreProducto, 120, "El nombre del producto");
        UnidadMedida = unidadMedida;
        EsFraccionado = esFraccionado;
        Cantidad = decimal.Round(cantidad, 3);
        if (Cantidad <= 0)
        {
            throw new DomainException("La cantidad utilizada debe ser mayor que cero.");
        }

        if (precioUnitario < 0 || costoUnitario < 0)
        {
            throw new DomainException("El precio y el costo unitario no pueden ser negativos.");
        }

        PrecioUnitario = decimal.Round(precioUnitario, 2);
        CostoUnitario = decimal.Round(costoUnitario, 2);
    }

    public Guid ServicioVehicularId { get; private init; }

    public ServicioVehicular ServicioVehicular { get; private init; } = null!;

    public Guid ProductoId { get; private init; }

    public string CodigoProducto { get; private set; } = string.Empty;

    public string NombreProducto { get; private set; } = string.Empty;

    public UnidadMedidaProducto UnidadMedida { get; private set; }

    public bool EsFraccionado { get; private set; }

    public decimal Cantidad { get; private set; }

    public decimal PrecioUnitario { get; private set; }

    public decimal CostoUnitario { get; private set; }

    public decimal Subtotal => decimal.Round(Cantidad * PrecioUnitario, 2);

    internal void ActualizarDatos(
        string codigoProducto,
        string nombreProducto,
        UnidadMedidaProducto unidadMedida,
        bool esFraccionado,
        decimal cantidad,
        decimal precioUnitario,
        decimal costoUnitario)
    {
        if (precioUnitario < 0 || costoUnitario < 0)
        {
            throw new DomainException("El precio y el costo unitario no pueden ser negativos.");
        }

        var normalizedQuantity = decimal.Round(cantidad, 3);
        if (normalizedQuantity <= 0)
        {
            throw new DomainException("La cantidad utilizada debe ser mayor que cero.");
        }

        CodigoProducto = Normalizar(codigoProducto, 40, "El codigo del producto");
        NombreProducto = Normalizar(nombreProducto, 120, "El nombre del producto");
        UnidadMedida = unidadMedida;
        EsFraccionado = esFraccionado;
        Cantidad = normalizedQuantity;
        PrecioUnitario = decimal.Round(precioUnitario, 2);
        CostoUnitario = decimal.Round(costoUnitario, 2);
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
