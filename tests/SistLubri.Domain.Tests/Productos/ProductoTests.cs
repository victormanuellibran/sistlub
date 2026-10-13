using SistLubri.Domain.Common;
using SistLubri.Domain.Productos;

namespace SistLubri.Domain.Tests.Productos;

public sealed class ProductoTests
{
    [Fact]
    public void Create_WithInitialStock_RecordsTheFirstMovement()
    {
        var producto = CreateProduct(stockInicial: 12.5m, stockMinimo: 5m);

        var movimiento = Assert.Single(producto.Movimientos);
        Assert.Equal(12.5m, producto.StockActual);
        Assert.Equal(TipoMovimientoInventario.CargaInicial, movimiento.Tipo);
        Assert.Equal(0m, movimiento.StockAnterior);
        Assert.Equal(12.5m, movimiento.StockResultante);
        Assert.Equal(EstadoInventario.Disponible, producto.EstadoInventario);
    }

    [Fact]
    public void RegisterMovement_UpdatesStockAndKeepsTheAuditValues()
    {
        var producto = CreateProduct(stockInicial: 10m, stockMinimo: 5m);

        var movimiento = producto.RegistrarMovimiento(
            TipoMovimientoInventario.Salida,
            6m,
            "Producto utilizado");

        Assert.Equal(4m, producto.StockActual);
        Assert.Equal(10m, movimiento.StockAnterior);
        Assert.Equal(4m, movimiento.StockResultante);
        Assert.Equal(EstadoInventario.BajoStock, producto.EstadoInventario);
    }

    [Fact]
    public void RegisterMovement_RejectsANegativeResult()
    {
        var producto = CreateProduct(stockInicial: 2m);

        Assert.Throws<DomainException>(() => producto.RegistrarMovimiento(
            TipoMovimientoInventario.Salida,
            3m,
            "Salida incorrecta"));
    }

    [Fact]
    public void Create_RequiresFractionalProductsToUseLiters()
    {
        Assert.Throws<DomainException>(() => new Producto(
            "ACE-01",
            "Aceite fraccionado",
            "Marca",
            "Aceites",
            "Tambor",
            UnidadMedidaProducto.Unidad,
            10m,
            5m,
            true));
    }

    [Fact]
    public void RegisterMovement_RejectsAnInactiveProduct()
    {
        var producto = CreateProduct();
        producto.CambiarEstado(false);

        Assert.Throws<DomainException>(() => producto.RegistrarMovimiento(
            TipoMovimientoInventario.Entrada,
            1m,
            "Reposicion"));
    }

    [Fact]
    public void RegisterMovement_RejectsAQuantityThatRoundsToZero()
    {
        var producto = CreateProduct();

        Assert.Throws<DomainException>(() => producto.RegistrarMovimiento(
            TipoMovimientoInventario.Entrada,
            0.0004m,
            "Cantidad sin precision suficiente"));
    }

    private static Producto CreateProduct(decimal stockInicial = 0, decimal stockMinimo = 0)
    {
        return new Producto(
            " ace-001 ",
            "Aceite 10W40",
            "Marca",
            "Aceites",
            "Bidon 4 L",
            UnidadMedidaProducto.Unidad,
            30_000m,
            20_000m,
            false,
            stockInicial,
            stockMinimo);
    }
}
