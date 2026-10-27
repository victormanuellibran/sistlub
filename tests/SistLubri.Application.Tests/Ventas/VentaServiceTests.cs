using SistLubri.Application.Caja;
using SistLubri.Application.Clientes;
using SistLubri.Application.Common.Persistence;
using SistLubri.Application.Productos;
using SistLubri.Application.Servicios;
using SistLubri.Application.Tests.Support;
using SistLubri.Application.Ventas;
using SistLubri.Domain.Caja;
using SistLubri.Domain.Clientes;
using SistLubri.Domain.Productos;
using SistLubri.Domain.Servicios;
using SistLubri.Domain.Ventas;

namespace SistLubri.Application.Tests.Ventas;

public sealed class VentaServiceTests
{
    [Fact]
    public async Task ChargeService_CollectsThePendingSaleCreatedAtCloseWithoutDiscountingStockAgain()
    {
        var user = new FakeCurrentUser();
        var client = new Cliente("Ana", "Gomez", "3515550101");
        var service = new ServicioVehicular(
            Guid.NewGuid(), client.Id, "AB123CD", "Ana Gomez", 50_000,
            "Cambio de aceite", null, null, null, 10_000m, user.Id);
        var product = new Producto("ACE-001", "Aceite 10W40", null, null, "Bidon 4 L",
            UnidadMedidaProducto.Unidad, 25_000m, 18_000m, false, 5m);
        service.AgregarOActualizarDetalle(product.Id, product.Codigo, product.Nombre,
            product.UnidadMedida, product.EsFraccionado, 1m, product.PrecioVenta,
            product.CostoCompra, user.Id);
        service.ConfirmarConsumo(new Dictionary<Guid, decimal> { [product.Id] = 1m }, user.Id);
        service.Finalizar(user.Id);

        var sale = Venta.CrearVentaDeServicio(service.Id, service.ClienteId, service.ClienteNombre,
            service.Patente, service.ManoObra, null, user.Id,
            [new DetalleVentaInicial(product.Id, product.Codigo, product.Nombre,
                product.Presentacion, product.UnidadMedida, product.EsFraccionado, 1m, product.PrecioVenta)]);
        var sales = new FakeVentaRepository();
        sales.Add(sale);
        var paymentMethod = new MedioPago("Efectivo", afectaEfectivo: true);
        var cashDay = JornadaCaja.Abrir(DateOnly.FromDateTime(DateTime.Today), 0, user.Id);
        var cashRepository = new FakeCajaRepository(cashDay, paymentMethod);
        var serviceRepository = new FakeServicioVehicularRepository(service);
        var productRepository = new FakeProductoRepository(product);
        var unitOfWork = new FakeUnitOfWork();
        var ventaService = new VentaService(
            sales,
            cashRepository,
            new FakeClienteRepository(client),
            productRepository,
            serviceRepository,
            unitOfWork,
            user);

        var charged = await ventaService.CobrarServicioAsync(service.Id,
            new CobrarServicioCommand(10_000m, 0m,
                [new PagoVentaCommand(paymentMethod.Id, 35_000m)], null));

        Assert.Equal(sale.Id, charged.Id);
        Assert.Equal(EstadoVenta.Pagada, sale.Estado);
        Assert.Equal(35_000m, cashDay.TotalIngresos);
        Assert.Single(sales.Items);
        Assert.DoesNotContain(product.Movimientos, movement => movement.Tipo == TipoMovimientoInventario.Salida);
        Assert.Equal(5m, product.StockActual);
        Assert.Equal(1, unitOfWork.SaveCount);
    }
}
