using SistLubri.Application.Caja;
using SistLubri.Application.Common.Exceptions;
using SistLubri.Application.Tests.Support;
using SistLubri.Domain.Caja;
using SistLubri.Domain.Common;

namespace SistLubri.Application.Tests.Caja;

public sealed class CajaServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task OpenAsync_PrimeraAperturaExigeRegistrarEfectivoContado()
    {
        var repository = new FakeCajaRepository();
        var service = CreateService(repository);

        await Assert.ThrowsAsync<DomainException>(() => service.OpenAsync(
            new AbrirCajaCommand(null), new DateOnly(2026, 10, 27)));
    }

    [Fact]
    public async Task OpenAsync_ArrastraSaldoContadoDeLaUltimaJornadaVerificada()
    {
        var previous = CreateVerifiedDay(new DateOnly(2026, 10, 22), 12_350m);
        var repository = new FakeCajaRepository(previous);
        var service = CreateService(repository);

        var opened = await service.OpenAsync(new AbrirCajaCommand(null), new DateOnly(2026, 10, 27));

        Assert.Equal(12_350m, opened.SaldoInicial);
        Assert.Equal(EstadoJornadaCaja.Abierta, opened.Estado);
    }

    [Fact]
    public async Task OpenAsync_NoPermiteEditarElSaldoArrastrado()
    {
        var previous = CreateVerifiedDay(new DateOnly(2026, 10, 26), 12_350m);
        var service = CreateService(new FakeCajaRepository(previous));

        await Assert.ThrowsAsync<ConflictException>(() => service.OpenAsync(
            new AbrirCajaCommand(20_000m), new DateOnly(2026, 10, 27)));
    }

    [Fact]
    public async Task OpenAsync_BloqueaSiLaJornadaAnteriorNoFueVerificada()
    {
        var previous = JornadaCaja.Abrir(new DateOnly(2026, 10, 26), 10_000, UserId);
        previous.Cerrar(UserId);
        var service = CreateService(new FakeCajaRepository(previous));

        await Assert.ThrowsAsync<ConflictException>(() => service.OpenAsync(
            new AbrirCajaCommand(null), new DateOnly(2026, 10, 27)));
    }

    [Fact]
    public async Task EjecutarAperturaAutomatica_NoCreaLaPrimeraJornadaEnCero()
    {
        var repository = new FakeCajaRepository
        {
            Configuracion = new ConfiguracionCaja(new TimeOnly(8, 0), new TimeOnly(18, 0), null, true, false)
        };
        var service = CreateService(repository);

        var opened = await service.EjecutarAperturaAutomaticaAsync(DateTimeOffset.Now);

        Assert.Null(opened);
        Assert.Empty(repository.Jornadas);
    }

    [Fact]
    public async Task EjecutarAperturaAutomatica_UsaSaldoContadoVerificado()
    {
        var previous = CreateVerifiedDay(DateOnly.FromDateTime(DateTime.Now).AddDays(-1), 12_350m);
        var repository = new FakeCajaRepository(previous)
        {
            Configuracion = new ConfiguracionCaja(new TimeOnly(0, 0), new TimeOnly(23, 59), null, true, false)
        };
        var service = CreateService(repository);

        var opened = await service.EjecutarAperturaAutomaticaAsync(DateTimeOffset.Now);

        Assert.Equal(12_350m, opened?.SaldoInicial);
    }

    [Fact]
    public async Task RegisterMovementAsync_NoPermiteRegistrarUnCobroFueraDelModuloVentas()
    {
        var service = CreateService(new FakeCajaRepository());

        await Assert.ThrowsAsync<DomainException>(() => service.RegisterMovementAsync(
            new MovimientoCajaCommand(TipoMovimientoCaja.CobroVenta, true, 500m, Guid.NewGuid(), null, null)));
    }

    private static JornadaCaja CreateVerifiedDay(DateOnly date, decimal counted)
    {
        var day = JornadaCaja.Abrir(date, 10_000m, UserId);
        day.Cerrar(UserId);
        day.Verificar(counted, counted == 10_000m ? null : "Diferencia de arqueo", UserId);
        return day;
    }

    private static CajaService CreateService(FakeCajaRepository repository) =>
        new(repository, new FakeUnitOfWork(), new FakeCurrentUser());
}
