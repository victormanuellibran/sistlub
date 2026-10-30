using SistLubri.Domain.Common;
using SistLubri.Domain.Compras;
using SistLubri.Domain.Productos;
using SistLubri.Domain.Proveedores;

namespace SistLubri.Domain.Tests.Compras;

public sealed class CompraTests
{
    [Fact]
    public void RegisterPartialReceipt_OnlyAcceptedQuantityIncreasesStockAndWeightedCost()
    {
        var userId = Guid.NewGuid();
        var provider = CreateProvider(userId);
        var product = CreateProduct(stock: 10m, cost: 100m);
        var order = CreateOrder(provider, product, userId);
        var detail = Assert.Single(order.Detalles);

        var receipt = order.RegistrarRecepcion(
            DateOnly.FromDateTime(DateTime.Today),
            "REM-001",
            null,
            [new LineaRecepcionInicial(detail.Id, 4m, 1m, 200m, "Una unidad dañada")],
            new Dictionary<Guid, Producto> { [product.Id] = product },
            userId);

        Assert.Equal(14m, product.StockActual);
        Assert.Equal(128.57m, product.CostoCompra);
        Assert.Equal(4m, detail.CantidadAceptada);
        Assert.Equal(6m, detail.CantidadPendiente);
        Assert.Equal(1m, detail.TotalRecepcionDanada);
        var purchaseMovement = product.Movimientos.Last();
        Assert.Equal(TipoMovimientoInventario.EntradaCompra, purchaseMovement.Tipo);
        Assert.Equal(receipt.Id, purchaseMovement.ReferenciaId);
        Assert.Equal(EstadoRecepcionCompra.Parcial, order.EstadoRecepcion);
    }

    [Fact]
    public void RegisterReceipt_RejectsAcceptedQuantityAboveRemainingOrderQuantity()
    {
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var order = CreateOrder(CreateProvider(userId), product, userId);
        var detail = Assert.Single(order.Detalles);

        Assert.Throws<DomainException>(() => order.RegistrarRecepcion(
            DateOnly.FromDateTime(DateTime.Today), null, null,
            [new LineaRecepcionInicial(detail.Id, 11m, 0m, 50m, null)],
            new Dictionary<Guid, Producto> { [product.Id] = product }, userId));

        Assert.Equal(0m, product.StockActual);
        Assert.Empty(product.Movimientos);
    }

    [Fact]
    public void Cancel_RejectsAnOrderThatAlreadyHasAReceipt()
    {
        var userId = Guid.NewGuid();
        var product = CreateProduct();
        var order = CreateOrder(CreateProvider(userId), product, userId);
        var detail = Assert.Single(order.Detalles);
        order.RegistrarRecepcion(DateOnly.FromDateTime(DateTime.Today), null, null,
            [new LineaRecepcionInicial(detail.Id, 1m, 0m, 50m, null)],
            new Dictionary<Guid, Producto> { [product.Id] = product }, userId);

        Assert.Throws<DomainException>(() => order.Anular("Cancelada", userId));
    }

    [Fact]
    public void UpdateBeforeReceipt_PreservesExistingDetailIdentity()
    {
        var userId = Guid.NewGuid();
        var provider = CreateProvider(userId);
        var product = CreateProduct();
        var order = CreateOrder(provider, product, userId);
        var originalDetail = Assert.Single(order.Detalles);

        order.ActualizarDatos(provider, DateOnly.FromDateTime(DateTime.Today), "Ajuste de cantidades",
            [new DetalleCompraInicial(product.Id, product.Codigo, product.Nombre,
                product.UnidadMedida.ToString(), 7m, 175m)], userId);

        var updatedDetail = Assert.Single(order.Detalles);
        Assert.Equal(originalDetail.Id, updatedDetail.Id);
        Assert.Equal(7m, updatedDetail.CantidadSolicitada);
        Assert.Equal(175m, updatedDetail.CostoUnitarioEstimado);
    }

    private static Compra CreateOrder(Proveedor provider, Producto product, Guid userId) => new(
        provider,
        DateOnly.FromDateTime(DateTime.Today),
        null,
        [new DetalleCompraInicial(product.Id, product.Codigo, product.Nombre,
            product.UnidadMedida.ToString(), 10m, 150m)],
        userId);

    private static Proveedor CreateProvider(Guid userId) => new(
        "Proveedor de prueba", null, null, "1112345678", null, null, null, null, null, null, null, userId);

    private static Producto CreateProduct(decimal stock = 0m, decimal cost = 50m) => new(
        "OIL-001", "Aceite 10W40", "Marca", "Aceites", "Bidon", UnidadMedidaProducto.Unidad,
        300m, cost, false, stock);
}
