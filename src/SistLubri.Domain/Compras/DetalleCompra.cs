using SistLubri.Domain.Common;

namespace SistLubri.Domain.Compras;

public sealed class DetalleCompra : Entity
{
    private DetalleCompra() { }
    internal DetalleCompra(Guid productoId, string codigo, string nombre, string unidad, decimal cantidad, decimal costo)
    {
        if (productoId == Guid.Empty || decimal.Round(cantidad, 3) <= 0 || costo < 0)
            throw new DomainException("El producto, cantidad y costo estimado de la linea no son validos.");
        ProductoId = productoId;
        CodigoProducto = Requerido(codigo, 40, "El codigo del producto");
        NombreProducto = Requerido(nombre, 120, "El nombre del producto");
        UnidadMedida = Requerido(unidad, 20, "La unidad de medida");
        CantidadSolicitada = decimal.Round(cantidad, 3);
        CostoUnitarioEstimado = decimal.Round(costo, 2);
    }
    public Guid CompraId { get; private set; }
    public Guid ProductoId { get; private set; }
    public string CodigoProducto { get; private set; } = string.Empty;
    public string NombreProducto { get; private set; } = string.Empty;
    public string UnidadMedida { get; private set; } = string.Empty;
    public decimal CantidadSolicitada { get; private set; }
    public decimal CantidadAceptada { get; private set; }
    public decimal CantidadPendiente => decimal.Round(CantidadSolicitada - CantidadAceptada, 3);
    public decimal CostoUnitarioEstimado { get; private set; }
    public decimal SubtotalEstimado => decimal.Round(CantidadSolicitada * CostoUnitarioEstimado, 2);
    public decimal TotalRecepcionDanada { get; private set; }
    internal void ActualizarDatos(DetalleCompraInicial item)
    {
        if (item.ProductoId != ProductoId || decimal.Round(item.CantidadSolicitada, 3) <= 0 || item.CostoUnitarioEstimado < 0)
            throw new DomainException("Los datos actualizados de la linea de compra no son validos.");
        CodigoProducto = Requerido(item.CodigoProducto, 40, "El codigo del producto");
        NombreProducto = Requerido(item.NombreProducto, 120, "El nombre del producto");
        UnidadMedida = Requerido(item.UnidadMedida, 20, "La unidad de medida");
        CantidadSolicitada = decimal.Round(item.CantidadSolicitada, 3);
        CostoUnitarioEstimado = decimal.Round(item.CostoUnitarioEstimado, 2);
    }
    internal void Aceptar(decimal cantidad) => CantidadAceptada = decimal.Round(CantidadAceptada + cantidad, 3);
    internal void RegistrarDanada(decimal cantidad) => TotalRecepcionDanada = decimal.Round(TotalRecepcionDanada + cantidad, 3);
    private static string Requerido(string? value, int max, string field)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > max)
            throw new DomainException($"{field} es obligatorio y no puede superar {max} caracteres.");
        return normalized;
    }
}
