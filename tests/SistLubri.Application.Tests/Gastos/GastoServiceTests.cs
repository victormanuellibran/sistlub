using SistLubri.Application.Common.Exceptions;
using SistLubri.Application.Gastos;
using SistLubri.Application.Tests.Support;
using SistLubri.Domain.Caja;
using SistLubri.Domain.Common;
using SistLubri.Domain.Gastos;

namespace SistLubri.Application.Tests.Gastos;

public sealed class GastoServiceTests
{
    private static readonly DateOnly Today = new(2026, 10, 27);

    [Fact]
    public async Task CreateAsync_SinPagoGuardaObligacionSinAfectarCaja()
    {
        var repo = new FakeGastoRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = Service(repo, new FakeCajaRepository(), unitOfWork);

        var result = await service.CreateAsync(Command());

        Assert.Equal(EstadoGasto.Pendiente, result.Estado);
        Assert.Equal(1000m, result.SaldoPendiente);
        Assert.Empty(result.Pagos);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateAsync_ConPagoMixtoRegistraPagosYMovimientosDeCajaVinculados()
    {
        var day = JornadaCaja.Abrir(Today, 500m, Guid.NewGuid());
        var cash = new MedioPago("Efectivo", afectaEfectivo: true);
        var transfer = new MedioPago("Transferencia", requiereReferencia: true);
        var repo = new FakeGastoRepository();
        var caja = new FakeCajaRepository(day, cash, transfer);
        var service = Service(repo, caja, new FakeUnitOfWork());
        var command = Command(pagos: [
            new(cash.Id, 100m, null, null),
            new(transfer.Id, 200m, "TR-001", null)], fechaPago: Today);

        var result = await service.CreateAsync(command);

        Assert.Equal(EstadoGasto.ParcialmentePagado, result.Estado);
        Assert.Equal(300m, result.TotalPagado);
        Assert.Equal(2, result.Pagos.Count);
        Assert.All(result.Pagos, payment => Assert.NotNull(payment.MovimientoCajaId));
        Assert.Equal(2, repo.AddedPayments.Count);
        Assert.Equal(2, caja.AddedMovements.Count);
        Assert.Equal(400m, day.EfectivoEsperado);
        Assert.Equal(2, day.Movimientos.Count);
    }

    [Fact]
    public async Task RegisterPaymentAsync_RegistraPagoYMovimientoComoNuevasEntidades()
    {
        var day = JornadaCaja.Abrir(Today, 500m, Guid.NewGuid());
        var transfer = new MedioPago("Transferencia", requiereReferencia: true);
        var repo = new FakeGastoRepository();
        var expense = CreateDomainExpense();
        repo.Add(expense);
        var caja = new FakeCajaRepository(day, transfer);
        var service = Service(repo, caja, new FakeUnitOfWork());

        var result = await service.RegisterPaymentAsync(expense.Id,
            new RegistrarPagosGastoCommand(Today, [new(transfer.Id, 250m, "TR-001", null)]));

        Assert.Equal(250m, result.TotalPagado);
        Assert.Single(repo.AddedPayments);
        var payment = Assert.Single(repo.AddedPayments);
        var movement = Assert.Single(caja.AddedMovements);
        Assert.Equal(payment.Id, movement.PagoGastoId);
        Assert.Equal(payment.Id, result.Pagos.Single().Id);
    }

    [Fact]
    public async Task RegisterPaymentAsync_RechazaEgresoEnEfectivoMayorAlDisponible()
    {
        var day = JornadaCaja.Abrir(Today, 50m, Guid.NewGuid());
        var cash = new MedioPago("Efectivo", afectaEfectivo: true);
        var repo = new FakeGastoRepository();
        var expense = CreateDomainExpense();
        repo.Add(expense);
        var service = Service(repo, new FakeCajaRepository(day, cash), new FakeUnitOfWork());

        await Assert.ThrowsAsync<DomainException>(() => service.RegisterPaymentAsync(expense.Id,
            new RegistrarPagosGastoCommand(Today, [new(cash.Id, 75m, null, null)])));
        Assert.Empty(day.Movimientos);
    }

    [Fact]
    public async Task CancelAsync_ReintegraPagoYAnulaSinBorrarHistorial()
    {
        var day = JornadaCaja.Abrir(Today, 500m, Guid.NewGuid());
        var transfer = new MedioPago("Transferencia", requiereReferencia: true);
        var repo = new FakeGastoRepository();
        var expense = CreateDomainExpense();
        expense.RegistrarPago(transfer.Id, 250m, Today, "TR-001", null, Guid.NewGuid());
        repo.Add(expense);
        var caja = new FakeCajaRepository(day, transfer);
        var service = Service(repo, caja, new FakeUnitOfWork());

        var result = await service.CancelAsync(expense.Id, new AnularGastoCommand("Gasto duplicado", Today,
            [new(transfer.Id, 250m, "DEV-001", null)]));

        Assert.Equal(EstadoGasto.Anulado, result.Estado);
        Assert.Single(result.Pagos);
        Assert.Single(result.Reintegros);
        Assert.Single(repo.AddedRefunds);
        Assert.Single(caja.AddedMovements);
        Assert.Equal(TipoMovimientoCaja.ReintegroGasto, Assert.Single(day.Movimientos).Tipo);
    }

    [Fact]
    public async Task CreateAsync_RechazaCompraDeMercaderiaFueraDelModuloCompras()
    {
        var service = Service(new FakeGastoRepository(), new FakeCajaRepository(), new FakeUnitOfWork());

        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(Command() with { EsCompraMercaderia = true }));
    }

    private static GastoService Service(FakeGastoRepository gastos, FakeCajaRepository caja, FakeUnitOfWork uow) =>
        new(gastos, new FakeProveedorRepository(), caja, uow, new FakeCurrentUser());

    private static CrearGastoCommand Command(
        IReadOnlyList<PagoGastoCommand>? pagos = null,
        DateOnly? fechaPago = null) =>
        new(CategoriaGasto.Servicios, null, "Factura de electricidad", Today, 1000m,
            "Empresa de energia", null, null, false, "FAC-001", null,
            fechaPago, pagos ?? [], false);

    private static Gasto CreateDomainExpense() =>
        new(CategoriaGasto.Servicios, null, "Factura de electricidad", Today, 1000m,
            null, null, null, false, null, null, Guid.NewGuid());

    private sealed class FakeGastoRepository(params Gasto[] expenses) : IGastoRepository
    {
        private readonly List<Gasto> _items = [.. expenses];
        public List<PagoGasto> AddedPayments { get; } = [];
        public List<ReintegroGasto> AddedRefunds { get; } = [];

        public Task<Gasto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.SingleOrDefault(item => item.Id == id));

        public Task<IReadOnlyList<Gasto>> SearchAsync(FiltroGastos filtro, DateOnly hoy,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Gasto>>(_items.ToArray());

        public void Add(Gasto gasto) => _items.Add(gasto);
        public void Add(PagoGasto pago) => AddedPayments.Add(pago);
        public void Add(ReintegroGasto reintegro) => AddedRefunds.Add(reintegro);
    }
}
