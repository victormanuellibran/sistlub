using SistLubri.Application.Common.Exceptions;
using SistLubri.Application.Productos;
using SistLubri.Application.Tests.Support;
using SistLubri.Domain.Productos;

namespace SistLubri.Application.Tests.Productos;

public sealed class ProductoServiceTests
{
    [Fact]
    public async Task Create_NormalizesCodeAndPersistsTheInitialStock()
    {
        var products = new FakeProductoRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = new ProductoService(products, unitOfWork);

        var result = await service.CreateAsync(new CrearProductoCommand(
            " ace 001 ",
            "Aceite 10W40",
            "Marca",
            "Aceites",
            "Bidon 4 L",
            UnidadMedidaProducto.Unidad,
            30_000m,
            20_000m,
            false,
            10m,
            3m));

        Assert.Equal("ACE001", result.Codigo);
        Assert.Equal(10m, result.StockActual);
        Assert.Single(products.Items.Single().Movimientos);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_RejectsADuplicateCode()
    {
        var existing = CreateProduct();
        var service = new ProductoService(new FakeProductoRepository(existing), new FakeUnitOfWork());

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(new CrearProductoCommand(
            "ACE-001",
            "Otro aceite",
            null,
            null,
            "Unidad",
            UnidadMedidaProducto.Unidad,
            1m,
            1m,
            false,
            0m,
            0m)));
    }

    [Fact]
    public async Task RegisterMovement_ReturnsTheResultingStock()
    {
        var product = CreateProduct(stockInicial: 8m);
        var unitOfWork = new FakeUnitOfWork();
        var service = new ProductoService(new FakeProductoRepository(product), unitOfWork);

        var result = await service.RegisterMovementAsync(
            product.Id,
            new RegistrarMovimientoInventarioCommand(
                TipoMovimientoInventario.Salida,
                2.5m,
                "Uso en servicio",
                Guid.NewGuid()));

        Assert.Equal(8m, result.StockAnterior);
        Assert.Equal(5.5m, result.StockResultante);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task SetMinimumStock_RejectsAnInactiveProduct()
    {
        var product = CreateProduct();
        product.CambiarEstado(false);
        var service = new ProductoService(new FakeProductoRepository(product), new FakeUnitOfWork());

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.SetMinimumStockAsync(product.Id, 3m));
    }

    [Fact]
    public async Task RegisterMovement_RejectsASecondInitialLoad()
    {
        var product = CreateProduct();
        var service = new ProductoService(new FakeProductoRepository(product), new FakeUnitOfWork());

        await Assert.ThrowsAsync<ConflictException>(() => service.RegisterMovementAsync(
            product.Id,
            new RegistrarMovimientoInventarioCommand(
                TipoMovimientoInventario.CargaInicial,
                2m,
                "Otra carga inicial",
                null)));
    }

    private static Producto CreateProduct(decimal stockInicial = 0)
    {
        return new Producto(
            "ACE-001",
            "Aceite 10W40",
            "Marca",
            "Aceites",
            "Bidon 4 L",
            UnidadMedidaProducto.Unidad,
            30_000m,
            20_000m,
            false,
            stockInicial,
            0m);
    }
}
