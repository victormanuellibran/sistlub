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
    public void ReserveStock_ReducesAvailableQuantityWithoutChangingPhysicalStock()
    {
        var product = CreateProduct(stockInicial: 5m);

        product.ReservarStock(3m);

        Assert.Equal(5m, product.StockActual);
        Assert.Equal(3m, product.StockReservado);
        Assert.Equal(2m, product.StockDisponible);
        Assert.Throws<DomainException>(() => product.ReservarStock(2.001m));
    }

    [Fact]
    public void RegisterMovement_CannotConsumeStockReservedForServices()
    {
        var product = CreateProduct(stockInicial: 5m);
        product.ReservarStock(4m);

        Assert.Throws<DomainException>(() => product.RegistrarMovimiento(
            TipoMovimientoInventario.Salida, 2m, "Venta de mostrador"));

        product.RegistrarMovimiento(TipoMovimientoInventario.Salida, 1m, "Venta de mostrador");
        Assert.Equal(4m, product.StockActual);
        Assert.Equal(4m, product.StockReservado);
        Assert.Equal(0m, product.StockDisponible);
    }

    [Fact]
    public void ReleaseReservation_RestoresAvailabilityAndCannotReleaseMoreThanReserved()
    {
        var product = CreateProduct(stockInicial: 5m);
        product.ReservarStock(3m);

        product.LiberarReserva(2m);

        Assert.Equal(1m, product.StockReservado);
        Assert.Equal(4m, product.StockDisponible);
        Assert.Throws<DomainException>(() => product.LiberarReserva(2m));
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

    [Fact]
    public void Update_RejectsChangingUnitAfterInventoryMovement()
    {
        var producto = CreateProduct(stockInicial: 1m);

        Assert.Throws<DomainException>(() => producto.ActualizarDatos(
            "Aceite 10W40",
            "Marca",
            "Aceites",
            "Bidon 4 L",
            UnidadMedidaProducto.Litro,
            30_000m,
            20_000m,
            false));
    }

    [Fact]
    public void Update_RejectsChangingFractionalModeAfterInventoryMovement()
    {
        var producto = new Producto(
            "ACE-002",
            "Aceite a granel",
            "Marca",
            "Aceites",
            "Tambor",
            UnidadMedidaProducto.Litro,
            30_000m,
            20_000m,
            false,
            10m);

        Assert.Throws<DomainException>(() => producto.ActualizarDatos(
            "Aceite a granel",
            "Marca",
            "Aceites",
            "Tambor",
            UnidadMedidaProducto.Litro,
            30_000m,
            20_000m,
            true));
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
