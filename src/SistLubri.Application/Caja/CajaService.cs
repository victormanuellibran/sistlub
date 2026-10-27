using SistLubri.Application.Common.Exceptions;
using SistLubri.Application.Common.Persistence;
using SistLubri.Application.Common.Security;
using SistLubri.Domain.Caja;

namespace SistLubri.Application.Caja;

public sealed class CajaService(ICajaRepository repository, IUnitOfWork unitOfWork, ICurrentUser currentUser)
{
    private static readonly Guid UsuarioSistemaId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public async Task<IReadOnlyList<MedioPagoDto>> ListPaymentMethodsAsync(CancellationToken cancellationToken = default) =>
        (await repository.GetMediosPagoAsync(cancellationToken)).Select(Map).ToArray();

    public async Task<MedioPagoDto> ConfigurePaymentMethodAsync(Guid? id, ConfigurarMedioPagoCommand command, CancellationToken cancellationToken = default)
    {
        if (await repository.ExistsPaymentMethodNameAsync(command.Nombre, id, cancellationToken))
            throw new ConflictException("Ya existe un medio de pago con ese nombre.");
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        MedioPago method;
        if (id.HasValue)
        {
            method = await repository.GetMedioPagoAsync(id.Value, cancellationToken)
                ?? throw new NotFoundException("No se encontro el medio de pago.");
            method.Actualizar(command.Nombre, command.RequiereReferencia, command.AfectaEfectivo, command.Activo);
        }
        else
        {
            method = new MedioPago(command.Nombre, command.RequiereReferencia, command.AfectaEfectivo);
            if (!command.Activo) method.Actualizar(command.Nombre, command.RequiereReferencia, command.AfectaEfectivo, false);
            repository.Add(method);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(method);
    }

    public async Task<ConfiguracionCaja> ConfigureAsync(ConfigurarCajaCommand command, CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var configuration = await repository.GetConfigurationAsync(cancellationToken);
        if (configuration is null)
        {
            configuration = new ConfiguracionCaja(command.HoraApertura, command.HoraCierre, command.DuracionJornada, command.AperturaAutomatica, command.CierreAutomatico);
            repository.Add(configuration);
        }
        else configuration.Actualizar(command.HoraApertura, command.HoraCierre, command.DuracionJornada, command.AperturaAutomatica, command.CierreAutomatico);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return configuration;
    }

    public async Task<JornadaCajaDto> OpenAsync(AbrirCajaCommand command, DateOnly? fecha = null, CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        if (await repository.HasOpenAsync(cancellationToken)) throw new ConflictException("Ya existe una jornada de caja abierta.");
        var day = fecha ?? DateOnly.FromDateTime(DateTime.Now);
        if (await repository.ExistsOnDateAsync(day, cancellationToken))
            throw new ConflictException("La jornada de esta fecha ya existe. Para operar nuevamente debe reabrirla.");
        var previous = await repository.GetMostRecentBeforeAsync(day, cancellationToken);
        decimal openingBalance;
        if (previous is not null)
        {
            if (previous.Estado != EstadoJornadaCaja.Verificada || !previous.SaldoContado.HasValue)
                throw new ConflictException("No se puede abrir: la ultima jornada anterior debe cerrarse y verificarse primero.");
            openingBalance = previous.SaldoContado.Value;
            if (command.SaldoInicial.HasValue && command.SaldoInicial.Value != openingBalance)
                throw new ConflictException("El saldo inicial se toma del ultimo efectivo contado y verificado; no puede modificarse al abrir.");
        }
        else
        {
            if (await repository.ExistsAnyAsync(cancellationToken))
                throw new ConflictException("No se puede crear una apertura anterior a jornadas ya registradas.");
            openingBalance = command.SaldoInicial
                ?? throw new Domain.Common.DomainException("La primera apertura requiere registrar el efectivo contado fisicamente.");
        }

        var jornada = JornadaCaja.Abrir(day, openingBalance, currentUser.Id, Guid.NewGuid());
        repository.Add(jornada);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(jornada);
    }

    public async Task<JornadaCajaDto?> EjecutarAperturaAutomaticaAsync(DateTimeOffset ahoraUtc, CancellationToken cancellationToken = default)
    {
        var configuration = await repository.GetConfigurationAsync(cancellationToken);
        if (configuration is null || !configuration.AperturaAutomatica) return null;
        var localNow = ahoraUtc.ToLocalTime();
        var localTime = TimeOnly.FromDateTime(localNow.DateTime);
        var date = DateOnly.FromDateTime(localNow.DateTime);
        var shiftDate = date;
        if (localTime < configuration.HoraApertura || await repository.ExistsOnDateAsync(shiftDate, cancellationToken)) return null;
        if (await repository.HasOpenAsync(cancellationToken))
            throw new ConflictException("Existe una jornada abierta que impide la apertura automatica.");
        if (await repository.ExistsOnDateAsync(shiftDate, cancellationToken)) return null;
        var openingBalance = await repository.GetMostRecentBeforeAsync(shiftDate, cancellationToken);
        if (openingBalance is null || openingBalance.Estado != EstadoJornadaCaja.Verificada || !openingBalance.SaldoContado.HasValue)
            return null;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var jornada = JornadaCaja.Abrir(shiftDate, openingBalance.SaldoContado.Value, UsuarioSistemaId, Guid.NewGuid());
        repository.Add(jornada);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(jornada);
    }

    public async Task<JornadaCajaDto?> EjecutarCierreAutomaticoAsync(DateTimeOffset ahoraUtc, CancellationToken cancellationToken = default)
    {
        var configuration = await repository.GetConfigurationAsync(cancellationToken);
        var current = await repository.GetActualAsync(cancellationToken);
        if (configuration is null || !configuration.CierreAutomatico || current is null) return null;
        var localNow = ahoraUtc.ToLocalTime();
        var localTime = TimeOnly.FromDateTime(localNow.DateTime);
        var shouldClose = configuration.DuracionJornada.HasValue
            ? localNow >= current.AbiertaEnUtc.ToLocalTime() + configuration.DuracionJornada.Value
            : localTime >= configuration.HoraCierre;
        if (!shouldClose) return null;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        current.Cerrar(UsuarioSistemaId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(current);
    }

    public async Task<JornadaCajaDto?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var jornada = await repository.GetActualAsync(cancellationToken);
        return jornada is null ? null : Map(jornada);
    }

    public Task<ConfiguracionCaja?> GetConfigurationAsync(CancellationToken cancellationToken = default) =>
        repository.GetConfigurationAsync(cancellationToken);

    public async Task<IReadOnlyList<JornadaCajaDto>> SearchAsync(DateOnly? desde, DateOnly? hasta, CancellationToken cancellationToken = default) =>
        (await repository.SearchAsync(desde, hasta, cancellationToken)).Select(Map).ToArray();

    public async Task<JornadaCajaDto> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Map(await repository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("No se encontro la jornada de caja."));

    public async Task<JornadaCajaDto> RegisterMovementAsync(MovimientoCajaCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Tipo is not (TipoMovimientoCaja.IngresoManual or TipoMovimientoCaja.EgresoManual or TipoMovimientoCaja.RetiroEfectivo or TipoMovimientoCaja.Ajuste))
            throw new Domain.Common.DomainException("Los cobros, anulaciones y reintegros se registran desde el modulo de ventas, no como movimientos manuales de caja.");

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var day = await repository.GetActualAsync(cancellationToken) ?? throw new ConflictException("No hay una jornada de caja abierta.");
        if (command.Tipo == TipoMovimientoCaja.RetiroEfectivo)
        {
            if (!command.MedioPagoId.HasValue) throw new Domain.Common.DomainException("El retiro debe indicar el medio de pago en efectivo.");
            var method = await repository.GetMedioPagoAsync(command.MedioPagoId.Value, cancellationToken)
                ?? throw new NotFoundException("No se encontro el medio de pago.");
            if (!method.Activo || !method.AfectaEfectivo) throw new ConflictException("El retiro solo puede realizarse desde un medio de pago que afecte el efectivo.");
        }

        if ((command.Tipo is TipoMovimientoCaja.IngresoManual or TipoMovimientoCaja.EgresoManual or TipoMovimientoCaja.Ajuste) && string.IsNullOrWhiteSpace(command.Motivo))
            throw new Domain.Common.DomainException("Los movimientos manuales y ajustes requieren un motivo.");

        var affectsCash = true;
        if (command.MedioPagoId.HasValue)
        {
            var paymentMethod = await repository.GetMedioPagoAsync(command.MedioPagoId.Value, cancellationToken)
                ?? throw new NotFoundException("No se encontro el medio de pago.");
            if (!paymentMethod.Activo) throw new ConflictException("El medio de pago esta inactivo.");
            affectsCash = paymentMethod.AfectaEfectivo;
        }
        var movement = day.RegistrarMovimiento(command.Tipo, command.EsIngreso, command.Importe, currentUser.Id,
            command.MedioPagoId, null, command.Referencia, command.Motivo, affectsCash);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(day);
    }

    public async Task<JornadaCajaDto> CloseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var day = await LoadAsync(id, cancellationToken);
        day.Cerrar(currentUser.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(day);
    }

    public async Task<JornadaCajaDto> VerifyAsync(Guid id, VerificarCajaCommand command, CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var day = await LoadAsync(id, cancellationToken);
        day.Verificar(command.SaldoContado, command.Observacion, currentUser.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(day);
    }

    public async Task<JornadaCajaDto> ReopenAsync(Guid id, ReabrirCajaCommand command, CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        if (await repository.HasOpenAsync(cancellationToken)) throw new ConflictException("Existe otra jornada abierta.");
        var day = await LoadAsync(id, cancellationToken);
        day.Reabrir(command.Motivo, currentUser.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(day);
    }

    private async Task<JornadaCaja> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("No se encontro la jornada de caja.");

    private static MedioPagoDto Map(MedioPago method) => new(method.Id, method.Nombre, method.RequiereReferencia, method.AfectaEfectivo, method.Activo);
    private static JornadaCajaDto Map(JornadaCaja day) => new(
        day.Id, day.Fecha, day.SaldoInicial, day.Estado, day.TotalIngresos, day.TotalEgresos, day.SaldoEsperado, day.EfectivoEsperado,
        day.SaldoContado, day.DiferenciaVerificacion, day.AbiertaEnUtc, day.AbiertaPorUsuarioId,
        day.CerradaEnUtc, day.CerradaPorUsuarioId, day.VerificadaEnUtc, day.VerificadaPorUsuarioId,
        day.ObservacionVerificacion, day.ReabiertaEnUtc, day.ReabiertaPorUsuarioId,
        day.MotivoReapertura, day.NumeroReaperturas,
        day.Movimientos.Select(m => new MovimientoCajaDto(m.Id, m.Tipo, m.EsIngreso, m.Importe,
            m.UsuarioId, m.MedioPagoId, m.VentaId, m.Referencia, m.Motivo, m.FechaUtc, m.AfectaEfectivo)).ToArray());
}

