using SistLubri.Domain.Common;

namespace SistLubri.Domain.Caja;

public sealed class JornadaCaja : Entity
{
    private readonly List<MovimientoCaja> _movimientos = [];
    private readonly List<RegistroCierreCaja> _cierres = [];
    private readonly List<RegistroAperturaCaja> _aperturas = [];

    private JornadaCaja() { }

    private JornadaCaja(DateOnly fecha, decimal saldoInicial, Guid usuarioId, Guid? jornadaId = null)
    {
        if (saldoInicial < 0 || usuarioId == Guid.Empty)
        {
            throw new DomainException("El saldo inicial y el usuario de apertura deben ser validos.");
        }

        if (jornadaId.HasValue) Id = jornadaId.Value;
        Fecha = fecha;
        SaldoInicial = decimal.Round(saldoInicial, 2);
        Estado = EstadoJornadaCaja.Abierta;
        AbiertaEnUtc = DateTimeOffset.UtcNow;
        AbiertaPorUsuarioId = usuarioId;
        _aperturas.Add(new RegistroAperturaCaja(Id, 1, AbiertaEnUtc, usuarioId, SaldoInicial));
    }

    public DateOnly Fecha { get; private init; }
    public decimal SaldoInicial { get; private init; }
    public EstadoJornadaCaja Estado { get; private set; }
    public DateTimeOffset AbiertaEnUtc { get; private init; }
    public DateTimeOffset? ActualizadaEnUtc { get; private set; }
    public Guid AbiertaPorUsuarioId { get; private init; }
    public DateTimeOffset? CerradaEnUtc { get; private set; }
    public Guid? CerradaPorUsuarioId { get; private set; }
    public DateTimeOffset? VerificadaEnUtc { get; private set; }
    public Guid? VerificadaPorUsuarioId { get; private set; }
    public DateTimeOffset? ReabiertaEnUtc { get; private set; }
    public Guid? ReabiertaPorUsuarioId { get; private set; }
    public string? MotivoReapertura { get; private set; }
    public decimal TotalIngresos => decimal.Round(_movimientos.Where(m => m.EsIngreso).Sum(m => m.Importe), 2);
    public decimal TotalEgresos => decimal.Round(_movimientos.Where(m => !m.EsIngreso).Sum(m => m.Importe), 2);
    public decimal SaldoEsperado => decimal.Round(SaldoInicial + TotalIngresos - TotalEgresos, 2);
    public decimal EfectivoEsperado
    {
        get
        {
            var ultimaApertura = _aperturas.OrderByDescending(apertura => apertura.Numero).FirstOrDefault();
            var fechaApertura = ultimaApertura?.FechaUtc ?? AbiertaEnUtc;
            var saldoApertura = ultimaApertura?.SaldoInicial ?? SaldoInicial;
            var movimientosDesdeApertura = _movimientos.Where(movement => movement.FechaUtc > fechaApertura && movement.AfectaEfectivo);
            return decimal.Round(saldoApertura +
                movimientosDesdeApertura.Where(movement => movement.EsIngreso).Sum(movement => movement.Importe) -
                movimientosDesdeApertura.Where(movement => !movement.EsIngreso).Sum(movement => movement.Importe), 2);
        }
    }
    public IReadOnlyCollection<MovimientoCaja> Movimientos => _movimientos.AsReadOnly();
    public IReadOnlyCollection<RegistroCierreCaja> Cierres => _cierres.AsReadOnly();
    public IReadOnlyCollection<RegistroAperturaCaja> Aperturas => _aperturas.AsReadOnly();
    public IReadOnlyCollection<MovimientoCaja> MovimientosPendientesPersistencia => _movimientos.Where(movement => movement.Id != Guid.Empty).ToArray();
    public IReadOnlyCollection<RegistroCierreCaja> CierresPendientesPersistencia => _cierres.Where(close => close.Id != Guid.Empty).ToArray();
    public IReadOnlyCollection<RegistroAperturaCaja> AperturasPendientesPersistencia => _aperturas.Where(open => open.Id != Guid.Empty).ToArray();

    public void AddMovement(MovimientoCaja movement)
    {
        if (movement.JornadaCajaId != Id || _movimientos.Any(existing => existing.Id == movement.Id))
            throw new DomainException("El movimiento no pertenece a esta jornada o ya fue agregado.");
        _movimientos.Add(movement);
    }

    public void AddClose(RegistroCierreCaja close)
    {
        if (close.JornadaCajaId != Id || _cierres.Any(existing => existing.Id == close.Id))
            throw new DomainException("El cierre no pertenece a esta jornada o ya fue agregado.");
        _cierres.Add(close);
    }

    public void AddOpening(RegistroAperturaCaja opening)
    {
        if (opening.JornadaCajaId != Id || _aperturas.Any(existing => existing.Id == opening.Id))
            throw new DomainException("La apertura no pertenece a esta jornada o ya fue agregada.");
        _aperturas.Add(opening);
    }

    public static JornadaCaja Abrir(DateOnly fecha, decimal saldoInicial, Guid usuarioId) =>
        new(fecha, saldoInicial, usuarioId);

    public static JornadaCaja Abrir(DateOnly fecha, decimal saldoInicial, Guid usuarioId, Guid jornadaId) =>
        jornadaId == Guid.Empty ? throw new DomainException("El identificador de jornada no es valido.") : new(fecha, saldoInicial, usuarioId, jornadaId);

    public MovimientoCaja RegistrarMovimiento(
        TipoMovimientoCaja tipo,
        bool esIngreso,
        decimal importe,
        Guid usuarioId,
        Guid? medioPagoId = null,
        Guid? ventaId = null,
        string? referencia = null,
        string? motivo = null,
        bool afectaEfectivo = true,
        Guid? gastoId = null,
        Guid? pagoGastoId = null,
        Guid? reintegroGastoId = null)
    {
        ValidarAbierta(usuarioId);
        var esEgresoGasto = tipo == TipoMovimientoCaja.EgresoGasto;
        var esReintegroGasto = tipo == TipoMovimientoCaja.ReintegroGasto;
        if (!Enum.IsDefined(tipo) || importe <= 0 ||
            ((tipo is TipoMovimientoCaja.CobroVenta or TipoMovimientoCaja.IngresoManual) && !esIngreso) ||
            ((tipo is TipoMovimientoCaja.EgresoManual or TipoMovimientoCaja.RetiroEfectivo) && esIngreso) ||
            (tipo == TipoMovimientoCaja.CobroVenta && (!medioPagoId.HasValue || !ventaId.HasValue)) ||
            (tipo != TipoMovimientoCaja.CobroVenta && tipo is not (TipoMovimientoCaja.AnulacionVenta or TipoMovimientoCaja.ReintegroVenta) && ventaId.HasValue) ||
            (tipo == TipoMovimientoCaja.CobroVenta && !medioPagoId.HasValue) ||
            (esEgresoGasto && (esIngreso || !medioPagoId.HasValue || !gastoId.HasValue || !pagoGastoId.HasValue || reintegroGastoId.HasValue)) ||
            (esReintegroGasto && (!esIngreso || !medioPagoId.HasValue || !gastoId.HasValue || !reintegroGastoId.HasValue || pagoGastoId.HasValue)) ||
            (!esEgresoGasto && !esReintegroGasto && (gastoId.HasValue || pagoGastoId.HasValue || reintegroGastoId.HasValue)))
        {
            throw new DomainException("El movimiento de caja no es valido o le faltan referencias requeridas.");
        }

        if (!esIngreso && afectaEfectivo && importe > EfectivoEsperado)
            throw new DomainException("El egreso supera el efectivo disponible en la caja.");

        var movimiento = new MovimientoCaja(
            Id, tipo, esIngreso, importe, usuarioId, medioPagoId, ventaId, referencia, motivo, afectaEfectivo,
            gastoId: gastoId, pagoGastoId: pagoGastoId, reintegroGastoId: reintegroGastoId);
        _movimientos.Add(movimiento);
        ActualizadaEnUtc = DateTimeOffset.UtcNow;
        return movimiento;
    }

    public void Cerrar(Guid usuarioId)
    {
        ValidarAbierta(usuarioId);
        var closedAt = DateTimeOffset.UtcNow;
        var close = new RegistroCierreCaja(Id, _cierres.Count + 1, EstadoJornadaCaja.Cerrada,
            closedAt, usuarioId, EfectivoEsperado, null, null, null, null);
        if (_cierres.Count > 0) _cierres[^1].VincularNuevoCierre(close.Id);
        _cierres.Add(close);
        NumeroCierres++;
        Estado = EstadoJornadaCaja.Cerrada;
        CerradaEnUtc = closedAt;
        CerradaPorUsuarioId = usuarioId;
    }

    public void Verificar(decimal saldoContado, string? observacion, Guid usuarioId)
    {
        if (Estado != EstadoJornadaCaja.Cerrada || usuarioId == Guid.Empty || saldoContado < 0)
        {
            throw new DomainException("Solo se puede verificar una jornada cerrada con un saldo contado valido.");
        }

        var counted = decimal.Round(saldoContado, 2);
        var difference = decimal.Round(counted - EfectivoEsperado, 2);
        var normalizedObservation = NormalizarOpcional(observacion, 500);
        if (difference != 0 && normalizedObservation is null)
            throw new DomainException("Debe explicar la diferencia entre el efectivo esperado y el contado.");
        SaldoContado = counted;
        DiferenciaVerificacion = difference;
        ObservacionVerificacion = normalizedObservation;
        VerificadaEnUtc = DateTimeOffset.UtcNow;
        VerificadaPorUsuarioId = usuarioId;
        var lastClose = _cierres[^1];
        lastClose.Verificar(SaldoContado.Value, DiferenciaVerificacion.Value,
            ObservacionVerificacion, VerificadaEnUtc.Value, usuarioId);
        Estado = EstadoJornadaCaja.Verificada;
    }

    public decimal? SaldoContado { get; private set; }
    public decimal? DiferenciaVerificacion { get; private set; }
    public string? ObservacionVerificacion { get; private set; }
    public int NumeroReaperturas { get; private set; }
    public int NumeroCierres { get; private set; }

    public void Reabrir(string motivo, Guid usuarioId)
    {
        if (Estado is not (EstadoJornadaCaja.Cerrada or EstadoJornadaCaja.Verificada) || usuarioId == Guid.Empty)
        {
            throw new DomainException("Solo una jornada cerrada o verificada puede reabrirse.");
        }

        MotivoReapertura = NormalizarRequerido(motivo, 500);
        ReabiertaEnUtc = DateTimeOffset.UtcNow;
        ReabiertaPorUsuarioId = usuarioId;
        NumeroReaperturas++;
        _cierres[^1].RegistrarReapertura(MotivoReapertura, ReabiertaEnUtc.Value, usuarioId);
        var saldoReapertura = SaldoContado ?? EfectivoEsperado;
        var apertura = new RegistroAperturaCaja(Id, NumeroReaperturas + 1, ReabiertaEnUtc.Value, usuarioId, saldoReapertura);
        _aperturas.Add(apertura);
        Estado = EstadoJornadaCaja.Reabierta;
    }

    private void ValidarAbierta(Guid usuarioId)
    {
        if (Estado is not (EstadoJornadaCaja.Abierta or EstadoJornadaCaja.Reabierta) || usuarioId == Guid.Empty)
        {
            throw new DomainException("La jornada de caja no esta abierta para operar.");
        }
    }

    private static string NormalizarRequerido(string? texto, int max)
    {
        var value = texto?.Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Length > max)
        {
            throw new DomainException("El motivo es obligatorio y no puede superar los 500 caracteres.");
        }
        return value;
    }

    private static string? NormalizarOpcional(string? texto, int max)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        return NormalizarRequerido(texto, max);
    }
}
