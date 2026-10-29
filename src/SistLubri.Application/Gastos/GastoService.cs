using SistLubri.Application.Caja;
using SistLubri.Application.Common.Exceptions;
using SistLubri.Application.Common.Persistence;
using SistLubri.Application.Common.Security;
using SistLubri.Application.Proveedores;
using SistLubri.Domain.Caja;
using SistLubri.Domain.Common;
using SistLubri.Domain.Gastos;

namespace SistLubri.Application.Gastos;

public sealed class GastoService(
    IGastoRepository gastoRepository,
    IProveedorRepository proveedorRepository,
    ICajaRepository cajaRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
{
    public static IReadOnlyList<CategoriaGastoDto> Categorias { get; } =
    [
        new(CategoriaGasto.Alquiler, "Alquiler"),
        new(CategoriaGasto.Servicios, "Servicios"),
        new(CategoriaGasto.Impuestos, "Impuestos"),
        new(CategoriaGasto.Sueldos, "Sueldos"),
        new(CategoriaGasto.Mantenimiento, "Mantenimiento"),
        new(CategoriaGasto.Combustible, "Combustible"),
        new(CategoriaGasto.Honorarios, "Honorarios"),
        new(CategoriaGasto.Seguros, "Seguros"),
        new(CategoriaGasto.Limpieza, "Limpieza"),
        new(CategoriaGasto.TelefoniaEInternet, "Telefonia e internet"),
        new(CategoriaGasto.Otro, "Otro")
    ];

    public async Task<GastoDetalleDto> CreateAsync(CrearGastoCommand command, CancellationToken cancellationToken = default)
    {
        if (command.EsCompraMercaderia)
            throw new DomainException("Las compras de mercaderia deben registrarse desde el modulo Compras, no como gastos.");
        ValidarDistribucion(command.PagosIniciales);
        var userId = ObtenerUsuarioActual();

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await ValidarProveedorActivoAsync(command.ProveedorId, cancellationToken);
        var gasto = new Gasto(command.Categoria, command.CategoriaDetalle, command.Descripcion, command.Fecha,
            command.ImporteTotal, command.EntidadRelacionada, command.ProveedorId, command.FechaVencimiento,
            command.VencimientoEstimado, command.ReferenciaComprobante, command.Observaciones, userId);
        gastoRepository.Add(gasto);

        if (command.PagosIniciales.Count > 0)
        {
            if (!command.FechaPagoInicial.HasValue)
                throw new DomainException("Debe indicar la fecha del pago inicial.");
            var jornada = await ObtenerCajaAbiertaAsync(cancellationToken);
            await RegistrarPagosEnCajaAsync(gasto, jornada, command.FechaPagoInicial.Value,
                command.PagosIniciales, userId, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await MapDetalleAsync(gasto, cancellationToken);
    }

    public async Task<IReadOnlyList<GastoResumenDto>> SearchAsync(FiltroGastos filtro, CancellationToken cancellationToken = default)
    {
        if ((filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value > filtro.Hasta.Value) ||
            (filtro.VencimientoDesde.HasValue && filtro.VencimientoHasta.HasValue && filtro.VencimientoDesde.Value > filtro.VencimientoHasta.Value) ||
            filtro.ImporteMinimo < 0 || filtro.ImporteMaximo < 0 ||
            (filtro.ImporteMinimo.HasValue && filtro.ImporteMaximo.HasValue && filtro.ImporteMinimo.Value > filtro.ImporteMaximo.Value))
            throw new DomainException("El rango de filtros no es valido.");

        var today = DateOnly.FromDateTime(DateTime.Now);
        var expenses = await gastoRepository.SearchAsync(filtro, today, cancellationToken);
        return expenses.Select(expense => MapResumen(expense, today)).ToArray();
    }

    public async Task<GastoDetalleDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var gasto = await GetGastoAsync(id, cancellationToken);
        return await MapDetalleAsync(gasto, cancellationToken);
    }

    public async Task<GastoDetalleDto> UpdateAsync(Guid id, ActualizarGastoCommand command, CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var gasto = await GetGastoAsync(id, cancellationToken);
        ValidarVersion(gasto, command.Version);
        await ValidarProveedorActivoAsync(command.ProveedorId, cancellationToken);
        gasto.ActualizarDatos(command.Categoria, command.CategoriaDetalle, command.Descripcion, command.Fecha,
            command.ImporteTotal, command.EntidadRelacionada, command.ProveedorId, command.FechaVencimiento,
            command.VencimientoEstimado, command.ReferenciaComprobante, command.Observaciones, ObtenerUsuarioActual());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await MapDetalleAsync(gasto, cancellationToken);
    }

    public async Task<GastoDetalleDto> RegisterPaymentAsync(
        Guid id,
        RegistrarPagosGastoCommand command,
        CancellationToken cancellationToken = default)
    {
        ValidarDistribucion(command.Pagos, requireAtLeastOne: true);
        var userId = ObtenerUsuarioActual();
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var gasto = await GetGastoAsync(id, cancellationToken);
        var jornada = await ObtenerCajaAbiertaAsync(cancellationToken);
        await RegistrarPagosEnCajaAsync(gasto, jornada, command.Fecha, command.Pagos, userId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await MapDetalleAsync(gasto, cancellationToken);
    }

    public async Task<GastoDetalleDto> CancelAsync(
        Guid id,
        AnularGastoCommand command,
        CancellationToken cancellationToken = default)
    {
        ValidarDistribucion(command.Reintegros);
        var userId = ObtenerUsuarioActual();
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var gasto = await GetGastoAsync(id, cancellationToken);

        if (gasto.TotalPagado > 0)
        {
            if (!command.FechaReintegro.HasValue)
                throw new DomainException("Debe indicar la fecha del reintegro para anular un gasto con pagos.");
            var totalReintegros = Decimal.Round(command.Reintegros.Sum(item => item.Importe), 2);
            if (totalReintegros != gasto.TotalPagado)
                throw new DomainException("Los reintegros deben cubrir exactamente el total pagado antes de anular el gasto.");

            var jornada = await ObtenerCajaAbiertaAsync(cancellationToken);
            await RegistrarReintegrosEnCajaAsync(gasto, jornada, command.FechaReintegro.Value,
                command.Reintegros, command.Motivo, userId, cancellationToken);
        }
        else if (command.Reintegros.Count > 0)
        {
            throw new DomainException("No se pueden registrar reintegros para un gasto sin pagos.");
        }

        gasto.Anular(command.Motivo, userId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await MapDetalleAsync(gasto, cancellationToken);
    }

    private async Task RegistrarPagosEnCajaAsync(
        Gasto gasto,
        JornadaCaja jornada,
        DateOnly fecha,
        IReadOnlyList<PagoGastoCommand> payments,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var validated = await ValidarMediosPagoAsync(payments, cancellationToken);
        foreach (var (payment, method) in validated)
        {
            var expensePayment = gasto.RegistrarPago(method.Id, payment.Importe, fecha,
                payment.Referencia, payment.Observacion, userId);
            var movement = jornada.RegistrarMovimiento(TipoMovimientoCaja.EgresoGasto, false,
                payment.Importe, userId, method.Id, referencia: payment.Referencia,
                motivo: $"Pago gasto {gasto.Id}: {method.Nombre}", afectaEfectivo: method.AfectaEfectivo,
                gastoId: gasto.Id, pagoGastoId: expensePayment.Id);
            expensePayment.VincularMovimientoCaja(movement);
            gastoRepository.Add(expensePayment);
            cajaRepository.Add(movement);
        }
    }

    private async Task RegistrarReintegrosEnCajaAsync(
        Gasto gasto,
        JornadaCaja jornada,
        DateOnly fecha,
        IReadOnlyList<PagoGastoCommand> refunds,
        string motivo,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var validated = await ValidarMediosPagoAsync(refunds, cancellationToken);
        foreach (var (refund, method) in validated)
        {
            var expenseRefund = gasto.RegistrarReintegro(method.Id, refund.Importe, fecha,
                refund.Referencia, refund.Observacion, userId);
            var movement = jornada.RegistrarMovimiento(TipoMovimientoCaja.ReintegroGasto, true,
                refund.Importe, userId, method.Id, referencia: refund.Referencia,
                motivo: $"Reintegro gasto {gasto.Id}: {motivo}", afectaEfectivo: method.AfectaEfectivo,
                gastoId: gasto.Id, reintegroGastoId: expenseRefund.Id);
            expenseRefund.VincularMovimientoCaja(movement);
            gastoRepository.Add(expenseRefund);
            cajaRepository.Add(movement);
        }
    }

    private async Task<IReadOnlyList<(PagoGastoCommand Payment, MedioPago Method)>> ValidarMediosPagoAsync(
        IReadOnlyList<PagoGastoCommand> payments,
        CancellationToken cancellationToken)
    {
        var validated = new List<(PagoGastoCommand, MedioPago)>(payments.Count);
        foreach (var payment in payments)
        {
            var method = await cajaRepository.GetMedioPagoAsync(payment.MedioPagoId, cancellationToken)
                ?? throw new NotFoundException("No se encontro uno de los medios de pago.");
            method.ValidarCobro(payment.Referencia);
            validated.Add((payment, method));
        }
        return validated;
    }

    private async Task<Dictionary<Guid, string>> GetPaymentMethodNamesAsync(CancellationToken cancellationToken)
    {
        var methods = await cajaRepository.GetMediosPagoAsync(cancellationToken);
        return methods.ToDictionary(method => method.Id, method => method.Nombre);
    }

    private async Task<GastoDetalleDto> MapDetalleAsync(Gasto gasto, CancellationToken cancellationToken)
    {
        var methods = await GetPaymentMethodNamesAsync(cancellationToken);
        string MethodName(Guid id) => methods.GetValueOrDefault(id, "Medio de pago eliminado");
        var today = DateOnly.FromDateTime(DateTime.Now);
        return new GastoDetalleDto(
            gasto.Id, gasto.Categoria, gasto.CategoriaDetalle, gasto.Descripcion, gasto.Fecha,
            gasto.ImporteTotal, gasto.TotalPagado, gasto.TotalReintegrado, gasto.SaldoPendiente,
            gasto.Estado, gasto.ObtenerSituacion(today), gasto.FechaVencimiento, gasto.VencimientoEstimado,
            gasto.EntidadRelacionada, gasto.ProveedorId, gasto.Proveedor?.RazonSocial,
            gasto.ReferenciaComprobante, gasto.Observaciones, gasto.CreadoEnUtc, gasto.CreadoPorUsuarioId,
            gasto.ActualizadoEnUtc, gasto.ActualizadoPorUsuarioId, gasto.AnuladoEnUtc,
            gasto.AnuladoPorUsuarioId, gasto.MotivoAnulacion, Convert.ToBase64String(gasto.RowVersion),
            gasto.Pagos.OrderBy(item => item.Fecha).ThenBy(item => item.CreadoEnUtc)
                .Select(item => new PagoGastoDto(item.Id, item.MedioPagoId, MethodName(item.MedioPagoId),
                    item.Importe, item.Fecha, item.Referencia, item.Observacion, item.UsuarioId,
                    item.CreadoEnUtc, item.MovimientoCajaId)).ToArray(),
            gasto.Reintegros.OrderBy(item => item.Fecha).ThenBy(item => item.CreadoEnUtc)
                .Select(item => new ReintegroGastoDto(item.Id, item.MedioPagoId, MethodName(item.MedioPagoId),
                    item.Importe, item.Fecha, item.Referencia, item.Observacion, item.UsuarioId,
                    item.CreadoEnUtc, item.MovimientoCajaId)).ToArray());
    }

    private static GastoResumenDto MapResumen(Gasto gasto, DateOnly today) => new(
        gasto.Id, gasto.Categoria, gasto.CategoriaDetalle, gasto.Descripcion, gasto.Fecha,
        gasto.ImporteTotal, gasto.TotalPagado, gasto.SaldoPendiente, gasto.Estado,
        gasto.ObtenerSituacion(today), gasto.FechaVencimiento, gasto.VencimientoEstimado,
        gasto.EntidadRelacionada, gasto.ProveedorId, gasto.Proveedor?.RazonSocial,
        gasto.CreadoEnUtc, gasto.CreadoPorUsuarioId);

    private async Task<Gasto> GetGastoAsync(Guid id, CancellationToken cancellationToken) =>
        await gastoRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("No se encontro el gasto solicitado.");

    private async Task ValidarProveedorActivoAsync(Guid? proveedorId, CancellationToken cancellationToken)
    {
        if (!proveedorId.HasValue) return;
        var supplier = await proveedorRepository.GetByIdAsync(proveedorId.Value, cancellationToken)
            ?? throw new NotFoundException("No se encontro el proveedor relacionado.");
        if (!supplier.Activo)
            throw new ConflictException("No se pueden asociar nuevos gastos a un proveedor inactivo.");
    }

    private async Task<JornadaCaja> ObtenerCajaAbiertaAsync(CancellationToken cancellationToken) =>
        await cajaRepository.GetActualAsync(cancellationToken)
            ?? throw new ConflictException("Se requiere una jornada de caja abierta para registrar pagos o reintegros.");

    private Guid ObtenerUsuarioActual() => currentUser.Id == Guid.Empty
        ? throw new DomainException("No se pudo identificar al usuario autenticado.")
        : currentUser.Id;

    private static void ValidarVersion(Gasto gasto, string encodedVersion)
    {
        byte[] version;
        try
        {
            version = Convert.FromBase64String(encodedVersion);
        }
        catch (FormatException)
        {
            throw new DomainException("La version del gasto no tiene un formato valido.");
        }

        if (!gasto.RowVersion.SequenceEqual(version))
            throw new ConflictException("El gasto fue modificado por otro usuario. Vuelva a cargarlo antes de guardar.");
    }

    private static void ValidarDistribucion(IReadOnlyList<PagoGastoCommand> payments, bool requireAtLeastOne = false)
    {
        if (payments is null) throw new DomainException("La lista de pagos es obligatoria.");
        if (requireAtLeastOne && payments.Count == 0)
            throw new DomainException("Debe registrar al menos un pago.");
        if (payments.Select(item => item.MedioPagoId).Distinct().Count() != payments.Count)
            throw new DomainException("Cada medio de pago debe aparecer una sola vez en una operacion combinada.");
        if (payments.Any(item => item.MedioPagoId == Guid.Empty || item.Importe <= 0))
            throw new DomainException("Todos los importes deben ser mayores que cero y tener un medio de pago.");
    }
}

public sealed record CategoriaGastoDto(SistLubri.Domain.Gastos.CategoriaGasto Valor, string Nombre);
