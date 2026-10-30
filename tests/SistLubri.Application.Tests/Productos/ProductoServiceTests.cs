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
                TipoMovimientoInventario.AjusteNegativo,
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
    public async Task SetMinimumStock_RecordsTheResponsibleUser()
    {
        var product = CreateProduct(stockInicial: 8m);
        var user = new FakeCurrentUser();
        var service = new ProductoService(new FakeProductoRepository(product), new FakeUnitOfWork(), user);

        var result = await service.SetMinimumStockAsync(product.Id, 3m);

        Assert.Equal(3m, result.StockMinimo);
        Assert.Equal(user.Id, product.StockMinimoActualizadoPorUsuarioId);
        Assert.NotNull(product.StockMinimoActualizadoEnUtc);
    }

    [Fact]
    public async Task SearchMovements_FiltersByDateAndType()
    {
        var product = CreateProduct(stockInicial: 8m);
        product.RegistrarMovimiento(TipoMovimientoInventario.AjusteNegativo, 2m, "Merma");
        var service = new ProductoService(new FakeProductoRepository(product), new FakeUnitOfWork());

        var result = await service.SearchMovementsAsync(
            product.Id,
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow),
            TipoMovimientoInventario.AjusteNegativo);

        var movement = Assert.Single(result);
        Assert.Equal("Ajuste", movement.Origen);
        Assert.Equal(6m, movement.StockResultante);
    }

    [Fact]
    public async Task RegisterMovement_RejectsOperationalMovementOutsideItsOwningModule()
    {
        var product = CreateProduct();
        var service = new ProductoService(new FakeProductoRepository(product), new FakeUnitOfWork());

        await Assert.ThrowsAsync<ConflictException>(() => service.RegisterMovementAsync(
            product.Id,
            new RegistrarMovimientoInventarioCommand(
                TipoMovimientoInventario.EntradaCompra,
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
