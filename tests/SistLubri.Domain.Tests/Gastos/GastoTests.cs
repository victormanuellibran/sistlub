using SistLubri.Domain.Caja;
using SistLubri.Domain.Common;
using SistLubri.Domain.Gastos;

namespace SistLubri.Domain.Tests.Gastos;

public sealed class GastoTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CashMethodId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 10, 27);

    [Fact]
    public void CrearYPagar_ActualizaEstadoYSaldo()
    {
        var gasto = Crear();

        Assert.Equal(EstadoGasto.Pendiente, gasto.Estado);
        Assert.Equal(1000m, gasto.SaldoPendiente);

        gasto.RegistrarPago(CashMethodId, 300m, Today, null, null, UserId);
        Assert.Equal(EstadoGasto.ParcialmentePagado, gasto.Estado);
        Assert.Equal(700m, gasto.SaldoPendiente);

        gasto.RegistrarPago(CashMethodId, 700m, Today, null, null, UserId);
        Assert.Equal(EstadoGasto.Pagado, gasto.Estado);
        Assert.Equal(0m, gasto.SaldoPendiente);
        Assert.Equal(SituacionGasto.Pagado, gasto.ObtenerSituacion(Today));
    }

    [Fact]
    public void RegistrarPago_RechazaSobrepagoYFechaAnteriorAlGasto()
    {
        var gasto = Crear();

        Assert.Throws<DomainException>(() => gasto.RegistrarPago(CashMethodId, 1000.01m, Today, null, null, UserId));
        Assert.Throws<DomainException>(() => gasto.RegistrarPago(CashMethodId, 10m, Today.AddDays(-1), null, null, UserId));
    }

    [Fact]
    public void ActualizarDatos_NoPermiteReducirDebajoDeLoPagado()
    {
        var gasto = Crear();
        gasto.RegistrarPago(CashMethodId, 500m, Today, null, null, UserId);

        Assert.Throws<DomainException>(() => gasto.ActualizarDatos(CategoriaGasto.Servicios, null,
            "Electricidad", Today, 400m, null, null, null, false, null, null, UserId));
    }

    [Fact]
    public void Anular_ExigeReintegrarTodosLosPagosYConservaElRegistro()
    {
        var gasto = Crear();
        gasto.RegistrarPago(CashMethodId, 300m, Today, null, null, UserId);
        Assert.Throws<DomainException>(() => gasto.Anular("Cancelacion", UserId));

        gasto.RegistrarReintegro(CashMethodId, 300m, Today, null, null, UserId);
        gasto.Anular("Cancelacion", UserId);

        Assert.Equal(EstadoGasto.Anulado, gasto.Estado);
        Assert.Single(gasto.Pagos);
        Assert.Single(gasto.Reintegros);
        Assert.Equal(0m, gasto.SaldoPendiente);
    }

    [Fact]
    public void ObtenerSituacion_IdentificaVencimientoProximoYVencido()
    {
        var gasto = Crear(fechaVencimiento: Today.AddDays(2));
        Assert.Equal(SituacionGasto.ProximoAVencer, gasto.ObtenerSituacion(Today));
        Assert.Equal(SituacionGasto.Vencido, gasto.ObtenerSituacion(Today.AddDays(3)));
    }

    [Fact]
    public void Crear_OtroExigeDetalleYNoAdmiteFechaPorDefecto()
    {
        Assert.Throws<DomainException>(() => Crear(categoria: CategoriaGasto.Otro));
        Assert.Throws<DomainException>(() => Crear(fecha: DateOnly.MinValue));
    }

    private static Gasto Crear(
        CategoriaGasto categoria = CategoriaGasto.Servicios,
        string? categoriaDetalle = null,
        DateOnly? fechaVencimiento = null,
        DateOnly? fecha = null) =>
        new(categoria, categoriaDetalle, "Factura de electricidad", fecha ?? Today, 1000m,
            "Empresa de energia", null, fechaVencimiento, false, null, null, UserId);
}
