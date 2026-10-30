namespace SistLubri.Domain.Productos;

public enum TipoMovimientoInventario
{
    CargaInicial = 1,
    Entrada = 2,
    Salida = 3,
    AjustePositivo = 4,
    AjusteNegativo = 5,
    EntradaCompra = 6,
    DevolucionProveedor = 7
}
