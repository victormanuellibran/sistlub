using SistLubri.Domain.Common;
using SistLubri.Domain.Productos;
using SistLubri.Domain.Ventas;

namespace SistLubri.Domain.Tests.Ventas;

public sealed class VentaTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid PaymentMethodId = Guid.NewGuid();

    [Fact]
    public void CreateDirectSale_AllowsUnidentifiedCustomerAndStartsPending()
    {
        var sale = Venta.CrearVentaDirecta(null, null, null, UserId);

        Assert.Equal(OrigenVenta.Productos, sale.Origen);
        Assert.Equal(EstadoVenta.PendienteDeCobro, sale.Estado);
        Assert.Null(sale.ClienteId);
        Assert.False(sale.InventarioDescontado);
        Assert.Equal(UserId, sale.CreadaPorUsuarioId);
    }

    [Fact]
    public void DirectSale_CalculatesLineAndGeneralDiscountsFromSnapshots()
    {
        var sale = CreateDirectSale();
        sale.AgregarOActualizarDetalle(
            ProductId,
            "ACE-001",
            "Aceite 10W40",
            "Bidon 4 L",
            UnidadMedidaProducto.Unidad,
            false,
            2,
            30_000m,
            1_000m,
            UserId);
        sale.AplicarDescuentoGeneral(2_000m, UserId);

        Assert.Equal(60_000m, sale.TotalBruto);
        Assert.Equal(3_000m, sale.DescuentoTotal);
        Assert.Equal(57_000m, sale.Total);
    }

    [Fact]
    public void RegisterPayment_RequiresInventoryMovementForDirectSale()
    {
        var sale = CreateDirectSaleWithProduct();

        Assert.Throws<DomainException>(() => sale.RegistrarCobro(
            [new RegistrarPagoVenta(PaymentMethodId, 30_000m)],
            UserId));

        Assert.Empty(sale.Pagos);
        Assert.Equal(EstadoVenta.PendienteDeCobro, sale.Estado);
    }

    [Fact]
    public void RegisterPayment_AcceptsCombinedMethodsWhenTheyMatchTotal()
    {
        var sale = CreateDirectSaleWithProduct();
        sale.MarcarInventarioDescontado(UserId);
        var secondMethodId = Guid.NewGuid();

        sale.RegistrarCobro(
        [
            new RegistrarPagoVenta(PaymentMethodId, 10_000m, "transferencia 123"),
            new RegistrarPagoVenta(secondMethodId, 20_000m, Observacion: "Efectivo")
        ], UserId);

        Assert.Equal(EstadoVenta.Pagada, sale.Estado);
        Assert.Equal(30_000m, sale.Pagos.Sum(pago => pago.Importe));
        Assert.Equal(2, sale.Pagos.Count);
        Assert.Equal(UserId, sale.PagadaPorUsuarioId);
        Assert.NotNull(sale.PagadaEnUtc);
    }

    [Fact]
    public void RegisterPayment_RejectsUnderpaymentWithoutChangingState()
    {
        var sale = CreateDirectSaleWithProduct();
        sale.MarcarInventarioDescontado(UserId);

        Assert.Throws<DomainException>(() => sale.RegistrarCobro(
            [new RegistrarPagoVenta(PaymentMethodId, 29_999m)],
            UserId));

        Assert.Equal(EstadoVenta.PendienteDeCobro, sale.Estado);
        Assert.Empty(sale.Pagos);
    }

    [Fact]
    public void ServiceSale_StartsWithInventoryAlreadyDiscountedAndIncludesLabor()
    {
        var sale = CreateServiceSale();

        Assert.Equal(OrigenVenta.ServicioVehicular, sale.Origen);
        Assert.Equal(EstadoVenta.PendienteDeCobro, sale.Estado);
        Assert.True(sale.InventarioDescontado);
        Assert.Equal(125_000m, sale.Total);
        Assert.NotNull(sale.ServicioId);
    }

    [Fact]
    public void ServiceSale_CanBePaidWithoutDiscountingInventoryAgain()
    {
        var sale = CreateServiceSale();

        sale.RegistrarCobro([new RegistrarPagoVenta(PaymentMethodId, 125_000m)], UserId);

        Assert.Equal(EstadoVenta.Pagada, sale.Estado);
        Assert.True(sale.InventarioDescontado);
    }

    [Fact]
    public void ServiceSale_AllowsCommercialChangesButPreservesTechnicalQuantity()
    {
        var sale = CreateServiceSale();

        sale.ActualizarImportesServicio(
            100_000m,
            new Dictionary<Guid, (decimal PrecioUnitario, decimal Descuento)>
            {
                [ProductId] = (30_000m, 5_000m)
            },
            2_000m,
            UserId);

        Assert.Equal(1m, Assert.Single(sale.Detalles).Cantidad);
        Assert.Equal(123_000m, sale.Total);
        Assert.True(sale.InventarioDescontado);
    }

    [Fact]
    public void ServiceSale_RejectsInvalidCommercialChangesWithoutPartiallyUpdatingTheSale()
    {
        var sale = CreateServiceSale();
        var previousUpdatedBy = sale.ActualizadaPorUsuarioId;

        Assert.Throws<DomainException>(() => sale.ActualizarImportesServicio(
            10_000m,
            new Dictionary<Guid, (decimal PrecioUnitario, decimal Descuento)>
            {
                [ProductId] = (20_000m, 0m)
            },
            31_000m,
            Guid.NewGuid()));

        Assert.Equal(125_000m, sale.Total);
        Assert.Equal(100_000m, sale.ImporteManoObra);
        Assert.Equal(25_000m, Assert.Single(sale.Detalles).PrecioUnitario);
        Assert.Equal(previousUpdatedBy, sale.ActualizadaPorUsuarioId);
    }

    [Fact]
    public void ServiceSale_RejectsChangingTechnicalLineQuantitiesOrRemovingThem()
    {
        var sale = CreateServiceSale();

        Assert.Throws<DomainException>(() => sale.EliminarDetalle(ProductId, UserId));
        Assert.Throws<DomainException>(() => sale.AgregarOActualizarDetalle(
            Guid.NewGuid(),
            "FIL-001",
            "Filtro",
            "Unidad",
            UnidadMedidaProducto.Unidad,
            false,
            1,
            10_000m,
            0,
            UserId));
    }

    [Fact]
    public void Anular_RequiresReasonAndCannotBeRepeated()
    {
        var sale = CreateDirectSaleWithProduct();

        Assert.Throws<DomainException>(() => sale.Anular(" ", UserId));
        sale.Anular("Venta duplicada", UserId);

        Assert.Equal(EstadoVenta.Anulada, sale.Estado);
        Assert.Equal("Venta duplicada", sale.MotivoAnulacion);
        Assert.NotNull(sale.AnuladaEnUtc);
        Assert.Throws<DomainException>(() => sale.Anular("Segundo intento", UserId));
    }

    [Fact]
    public void Return_AllowsMultiplePartialRefundsButNeverExceedsOriginalPayment()
    {
        var sale = CreateDirectSaleWithProduct();
        sale.MarcarInventarioDescontado(UserId);
        sale.RegistrarCobro([new RegistrarPagoVenta(PaymentMethodId, 30_000m)], UserId);
        sale.EstablecerNombresMediosPago(["Efectivo"]);
        sale.EstablecerAfectacionEfectivoPagos([true]);

        sale.ReemplazarPagosPorReintegro([new RegistrarPagoVenta(PaymentMethodId, 10_000m)], "Devolucion parcial", UserId);
        Assert.Equal(EstadoVenta.DevueltaParcialmente, sale.Estado);
        Assert.False(Assert.Single(sale.Reintegros).EsAnulacion);
        sale.ReemplazarPagosPorReintegro([new RegistrarPagoVenta(PaymentMethodId, 20_000m)], "Saldo devuelto", UserId);

        Assert.Equal(EstadoVenta.Devuelta, sale.Estado);
        Assert.Equal(30_000m, sale.Reintegros.Sum(refund => refund.Importe));
        Assert.All(sale.Reintegros, refund => Assert.False(refund.EsAnulacion));
        Assert.Throws<DomainException>(() => sale.ReemplazarPagosPorReintegro(
            [new RegistrarPagoVenta(PaymentMethodId, 1m)], "Reintegro excedido", UserId));
    }

    [Fact]
    public void CancelPaidSale_RecordsFullReversalSeparatelyFromReturns()
    {
        var sale = CreateDirectSaleWithProduct();
        sale.MarcarInventarioDescontado(UserId);
        sale.RegistrarCobro(
        [
            new RegistrarPagoVenta(PaymentMethodId, 20_000m),
            new RegistrarPagoVenta(Guid.NewGuid(), 10_000m)
        ], UserId);

        sale.RegistrarAnulacionCobro(
        [
            new RegistrarPagoVenta(PaymentMethodId, 20_000m),
            new RegistrarPagoVenta(sale.Pagos.Last().MedioPagoId, 10_000m)
        ], "Venta duplicada", UserId);

        Assert.Equal(EstadoVenta.Anulada, sale.Estado);
        Assert.Equal("Venta duplicada", sale.MotivoAnulacion);
        Assert.All(sale.Reintegros, reversal => Assert.True(reversal.EsAnulacion));
        Assert.Equal(sale.Pagos.Sum(payment => payment.Importe), sale.Reintegros.Sum(reversal => reversal.Importe));
        Assert.Null(sale.MotivoDevolucion);
    }

    [Fact]
    public void Detail_ReturnQuantityIsCumulative()
    {
        var sale = CreateDirectSaleWithProduct();
        var detail = Assert.Single(sale.Detalles);
        detail.RegistrarDevolucion(0.4m);
        detail.RegistrarDevolucion(0.6m);

        Assert.Equal(1m, detail.CantidadDevuelta);
        Assert.Throws<DomainException>(() => detail.RegistrarDevolucion(0.01m));
    }

    private static Venta CreateDirectSale()
    {
        return Venta.CrearVentaDirecta(null, null, null, UserId);
    }

    private static Venta CreateDirectSaleWithProduct()
    {
        var sale = CreateDirectSale();
        sale.AgregarOActualizarDetalle(
            ProductId,
            "ACE-001",
            "Aceite 10W40",
            "Bidon 4 L",
            UnidadMedidaProducto.Unidad,
            false,
            1,
            30_000m,
            0,
            UserId);
        return sale;
    }

    private static Venta CreateServiceSale()
    {
        return Venta.CrearVentaDeServicio(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Ana Gomez",
            "AB123CD",
            100_000m,
            null,
            UserId,
            [new DetalleVentaInicial(
                ProductId,
                "ACE-001",
                "Aceite 10W40",
                "Bidon 4 L",
                UnidadMedidaProducto.Unidad,
                false,
                1,
                25_000m)]);
    }
}
