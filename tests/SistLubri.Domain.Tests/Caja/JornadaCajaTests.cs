using SistLubri.Domain.Caja;
using SistLubri.Domain.Common;
using System.Reflection;

namespace SistLubri.Domain.Tests.Caja;

public sealed class JornadaCajaTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Abrir_ExigeUsuarioYSaldoNoNegativo()
    {
        Assert.Throws<DomainException>(() => JornadaCaja.Abrir(DateOnly.FromDateTime(DateTime.Today), -1, UserId));
        Assert.Throws<DomainException>(() => JornadaCaja.Abrir(DateOnly.FromDateTime(DateTime.Today), 0, Guid.Empty));
    }

    [Fact]
    public void RegistrarMovimiento_CalculaSaldoYNoAceptaSignoInconsistente()
    {
        var jornada = JornadaCaja.Abrir(DateOnly.FromDateTime(DateTime.Today), 10_000, UserId);
        jornada.RegistrarMovimiento(TipoMovimientoCaja.IngresoManual, true, 2_500, UserId);
        jornada.RegistrarMovimiento(TipoMovimientoCaja.RetiroEfectivo, false, 500, UserId);

        Assert.Equal(12_000, jornada.SaldoEsperado);
        Assert.Throws<DomainException>(() => jornada.RegistrarMovimiento(TipoMovimientoCaja.EgresoManual, true, 100, UserId));
    }

    [Fact]
    public void RegistrarEgreso_NoPermiteRetirarMasEfectivoQueElDisponible()
    {
        var jornada = JornadaCaja.Abrir(DateOnly.FromDateTime(DateTime.Today), 500, UserId);

        Assert.Throws<DomainException>(() => jornada.RegistrarMovimiento(
            TipoMovimientoCaja.RetiroEfectivo, false, 501, UserId));
        Assert.Equal(500, jornada.EfectivoEsperado);
    }

    [Fact]
    public void Abrir_RegistroInicialPerteneceALaJornadaConIdentificadorExplicito()
    {
        var id = Guid.NewGuid();
        var jornada = JornadaCaja.Abrir(DateOnly.FromDateTime(DateTime.Today), 0, UserId, id);

        Assert.Equal(id, jornada.Id);
        Assert.Equal(id, Assert.Single(jornada.Aperturas).JornadaCajaId);
    }

    [Fact]
    public void EfectivoEsperado_SeReconstruyeDesdeLaAperturaPersistida()
    {
        var jornada = JornadaCaja.Abrir(DateOnly.FromDateTime(DateTime.Today), 700_000, UserId);
        var apertura = Assert.Single(jornada.Aperturas);
        var rehidratada = (JornadaCaja)Activator.CreateInstance(typeof(JornadaCaja), nonPublic: true)!;

        typeof(JornadaCaja).GetProperty(nameof(JornadaCaja.SaldoInicial))!
            .SetValue(rehidratada, jornada.SaldoInicial);
        typeof(JornadaCaja).GetProperty(nameof(JornadaCaja.AbiertaEnUtc))!
            .SetValue(rehidratada, jornada.AbiertaEnUtc);
        var aperturas = (List<RegistroAperturaCaja>)typeof(JornadaCaja)
            .GetField("_aperturas", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(rehidratada)!;
        aperturas.Add(apertura);

        Assert.Equal(700_000, rehidratada.EfectivoEsperado);
    }

    [Fact]
    public void Verificar_DeterminaDiferenciaContraEfectivoFisico()
    {
        var jornada = JornadaCaja.Abrir(DateOnly.FromDateTime(DateTime.Today), 10_000, UserId);
        jornada.RegistrarMovimiento(TipoMovimientoCaja.IngresoManual, true, 5_000, UserId);
        jornada.Cerrar(UserId);
        jornada.Verificar(14_500, "Faltan 500", UserId);

        Assert.Equal(15_000, jornada.EfectivoEsperado);
        Assert.Equal(-500, jornada.DiferenciaVerificacion);
        Assert.Equal(EstadoJornadaCaja.Verificada, jornada.Estado);
    }

    [Fact]
    public void Verificar_ExigeObservacionCuandoHayDiferencia()
    {
        var jornada = JornadaCaja.Abrir(DateOnly.FromDateTime(DateTime.Today), 10_000, UserId);
        jornada.Cerrar(UserId);

        Assert.Throws<DomainException>(() => jornada.Verificar(9_000, null, UserId));
        Assert.Equal(EstadoJornadaCaja.Cerrada, jornada.Estado);
        Assert.Null(jornada.SaldoContado);
        jornada.Verificar(9_000, "Faltante contado", UserId);
        Assert.Equal(-1_000, jornada.DiferenciaVerificacion);
    }

    [Fact]
    public void Verificar_NoExigeObservacionCuandoNoHayDiferencia()
    {
        var jornada = JornadaCaja.Abrir(DateOnly.FromDateTime(DateTime.Today), 10_000, UserId);
        jornada.Cerrar(UserId);

        jornada.Verificar(10_000, null, UserId);

        Assert.Equal(EstadoJornadaCaja.Verificada, jornada.Estado);
        Assert.Null(jornada.ObservacionVerificacion);
    }

    [Fact]
    public void Reabrir_ExigeMotivoYPermiteOperarConHistorial()
    {
        var jornada = JornadaCaja.Abrir(DateOnly.FromDateTime(DateTime.Today), 0, UserId);
        jornada.Cerrar(UserId);
        Assert.Throws<DomainException>(() => jornada.Reabrir(" ", UserId));
        jornada.Reabrir("Revisar arqueo", UserId);
        jornada.RegistrarMovimiento(TipoMovimientoCaja.IngresoManual, true, 800, UserId);

        Assert.Equal(1, jornada.NumeroReaperturas);
        Assert.Equal(800, jornada.SaldoEsperado);
    }

    [Fact]
    public void JornadaCerrada_NoAceptaMovimientos()
    {
        var jornada = JornadaCaja.Abrir(DateOnly.FromDateTime(DateTime.Today), 0, UserId);
        jornada.Cerrar(UserId);

        Assert.Throws<DomainException>(() => jornada.RegistrarMovimiento(TipoMovimientoCaja.IngresoManual, true, 100, UserId));
        Assert.Throws<DomainException>(() => jornada.Cerrar(UserId));
    }
}
