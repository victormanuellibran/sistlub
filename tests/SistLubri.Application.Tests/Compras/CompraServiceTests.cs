using SistLubri.Application.Caja;
using SistLubri.Application.Compras;
using SistLubri.Application.Tests.Support;
using SistLubri.Domain.Caja;
using SistLubri.Domain.Compras;
using SistLubri.Domain.Productos;
using SistLubri.Domain.Proveedores;

namespace SistLubri.Application.Tests.Compras;

public sealed class CompraServiceTests
{
    [Fact]
    public async Task CreateAsync_ReturnsSupplierNameAndDoesNotChangeStock()
    {
        var user = new FakeCurrentUser();
        var supplier = new Proveedor("Proveedor de prueba", null, null, "1112345678", null,
            null, null, null, null, null, null, user.Id);
        var product = new Producto("OIL-01", "Aceite 10W40", null, "Aceites", "Bidon",
            UnidadMedidaProducto.Unidad, 300m, 100m, false, 5m);
        var purchases = new FakeCompraRepository();
        var service = new CompraService(purchases, new FakeProductoRepository(product),
            new FakeProveedorRepository(supplier), new FakeCajaRepository(), new FakeUnitOfWork(), user);

        var result = await service.CreateAsync(new CrearCompraCommand(supplier.Id,
            DateOnly.FromDateTime(DateTime.Today), null,
            [new DetalleCompraCommand(product.Id, 2m, 120m)]));

        Assert.Equal("Proveedor de prueba", result.NombreProveedor);
        Assert.Equal(5m, product.StockActual);
        var saved = Assert.Single(purchases.Items);
        Assert.Equal(result.Id, saved.Id);
        Assert.Equal(EstadoRecepcionCompra.Pendiente, result.EstadoRecepcion);
    }

    private sealed class FakeCompraRepository : ICompraRepository
    {
        public List<Compra> Items { get; } = [];
        public Task<Compra?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.SingleOrDefault(item => item.Id == id));
        public Task<IReadOnlyList<Compra>> SearchAsync(DateOnly? desde, DateOnly? hasta, Guid? proveedorId,
            EstadoRecepcionCompra? recepcion, EstadoPagoCompra? pago, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Compra>>(Items.ToArray());
        public Task<bool> ExistsInvoiceNumberAsync(Guid proveedorId, string numero, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public void Add(Compra compra) => Items.Add(compra);
        public void Add(RecepcionCompra recepcion) { }
        public void Add(FacturaCompra factura) { }
        public void Add(PagoCompra pago) { }
    }
}
