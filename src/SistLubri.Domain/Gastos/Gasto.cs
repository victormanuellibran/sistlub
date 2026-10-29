using SistLubri.Domain.Common;
using SistLubri.Domain.Proveedores;

namespace SistLubri.Domain.Gastos;

public sealed class Gasto : Entity
{
    private readonly List<PagoGasto> _pagos = [];
    private readonly List<ReintegroGasto> _reintegros = [];
    private EstadoGasto _estado = EstadoGasto.Pendiente;

    private Gasto() { }

    public Gasto(
        CategoriaGasto categoria,
        string? categoriaDetalle,
        string descripcion,
        DateOnly fecha,
        decimal importeTotal,
        string? entidadRelacionada,
        Guid? proveedorId,
        DateOnly? fechaVencimiento,
        bool vencimientoEstimado,
        string? referenciaComprobante,
        string? observaciones,
        Guid usuarioId)
    {
        if (usuarioId == Guid.Empty) throw new DomainException("El usuario responsable no es valido.");
        CreadoEnUtc = DateTimeOffset.UtcNow;
        CreadoPorUsuarioId = usuarioId;
        ActualizadoPorUsuarioId = usuarioId;
        ActualizadoEnUtc = CreadoEnUtc;
        ActualizarDatos(categoria, categoriaDetalle, descripcion, fecha, importeTotal, entidadRelacionada,
            proveedorId, fechaVencimiento, vencimientoEstimado, referenciaComprobante, observaciones, usuarioId, true);
    }

    public CategoriaGasto Categoria { get; private set; }
    public string? CategoriaDetalle { get; private set; }
    public string Descripcion { get; private set; } = string.Empty;
    public DateOnly Fecha { get; private set; }
    public decimal ImporteTotal { get; private set; }
    public string? EntidadRelacionada { get; private set; }
    public Guid? ProveedorId { get; private set; }
    public Proveedor? Proveedor { get; private set; }
    public DateOnly? FechaVencimiento { get; private set; }
    public bool VencimientoEstimado { get; private set; }
    public string? ReferenciaComprobante { get; private set; }
    public string? Observaciones { get; private set; }
    public DateTimeOffset CreadoEnUtc { get; private init; }
    public Guid CreadoPorUsuarioId { get; private init; }
    public DateTimeOffset ActualizadoEnUtc { get; private set; }
    public Guid ActualizadoPorUsuarioId { get; private set; }
    public DateTimeOffset? AnuladoEnUtc { get; private set; }
    public Guid? AnuladoPorUsuarioId { get; private set; }
    public string? MotivoAnulacion { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<PagoGasto> Pagos => _pagos.AsReadOnly();
    public IReadOnlyCollection<ReintegroGasto> Reintegros => _reintegros.AsReadOnly();
    public IReadOnlyCollection<PagoGasto> PagosPendientesPersistencia => _pagos.Where(item => item.Id != Guid.Empty).ToArray();
    public IReadOnlyCollection<ReintegroGasto> ReintegrosPendientesPersistencia => _reintegros.Where(item => item.Id != Guid.Empty).ToArray();
    public decimal TotalPagado => decimal.Round(_pagos.Sum(item => item.Importe), 2);
    public decimal TotalReintegrado => decimal.Round(_reintegros.Sum(item => item.Importe), 2);
    public decimal SaldoPendiente => Estado == EstadoGasto.Anulado ? 0 : decimal.Round(ImporteTotal - TotalPagado, 2);
    public EstadoGasto Estado { get => _estado; private set => _estado = value; }

    public SituacionGasto ObtenerSituacion(DateOnly hoy, int diasProximoVencimiento = 7)
    {
        if (Estado == EstadoGasto.Anulado) return SituacionGasto.Anulado;
        if (Estado == EstadoGasto.Pagado) return SituacionGasto.Pagado;
        if (FechaVencimiento.HasValue && FechaVencimiento.Value < hoy) return SituacionGasto.Vencido;
        if (FechaVencimiento.HasValue && FechaVencimiento.Value <= hoy.AddDays(diasProximoVencimiento))
            return SituacionGasto.ProximoAVencer;
        return Estado == EstadoGasto.ParcialmentePagado
            ? SituacionGasto.ParcialmentePagado
            : SituacionGasto.Pendiente;
    }

    public void ActualizarDatos(
        CategoriaGasto categoria,
        string? categoriaDetalle,
        string descripcion,
        DateOnly fecha,
        decimal importeTotal,
        string? entidadRelacionada,
        Guid? proveedorId,
        DateOnly? fechaVencimiento,
        bool vencimientoEstimado,
        string? referenciaComprobante,
        string? observaciones,
        Guid usuarioId,
        bool esAlta = false)
    {
        if (!esAlta && Estado is EstadoGasto.Pagado or EstadoGasto.Anulado)
            throw new DomainException("Un gasto pagado o anulado no se puede modificar.");
        if (usuarioId == Guid.Empty || proveedorId == Guid.Empty)
            throw new DomainException("El usuario o proveedor relacionado no es valido.");
        if (!Enum.IsDefined(categoria)) throw new DomainException("La categoria de gasto no es valida.");
        if (fecha == default || (fechaVencimiento.HasValue && fechaVencimiento.Value == default))
            throw new DomainException("Las fechas del gasto no son validas.");
        if (importeTotal <= 0) throw new DomainException("El importe total debe ser mayor que cero.");

        var normalizedTotal = decimal.Round(importeTotal, 2);
        if (!esAlta && normalizedTotal < TotalPagado)
            throw new DomainException("El importe total no puede ser menor que lo ya pagado.");
        if (!esAlta && fecha > _pagos.Select(item => item.Fecha).DefaultIfEmpty(fecha).Min())
            throw new DomainException("La fecha del gasto no puede ser posterior a la fecha de un pago ya registrado.");
        if (fechaVencimiento.HasValue && fechaVencimiento.Value < fecha)
            throw new DomainException("El vencimiento no puede ser anterior a la fecha del gasto.");
        if (vencimientoEstimado && !fechaVencimiento.HasValue)
            throw new DomainException("Debe indicar la fecha estimada de vencimiento.");

        var detail = Normalizar(categoriaDetalle, 120, "El detalle de categoria");
        if (categoria == CategoriaGasto.Otro && detail is null)
            throw new DomainException("Debe detallar la categoria Otro.");
        if (categoria != CategoriaGasto.Otro && detail is not null)
            throw new DomainException("El detalle adicional solo corresponde a la categoria Otro.");

        Categoria = categoria;
        CategoriaDetalle = detail;
        Descripcion = NormalizarRequerido(descripcion, 500, "La descripcion");
        Fecha = fecha;
        ImporteTotal = normalizedTotal;
        EntidadRelacionada = Normalizar(entidadRelacionada, 200, "La entidad relacionada");
        ProveedorId = proveedorId;
        FechaVencimiento = _estado == EstadoGasto.Pagado ? null : fechaVencimiento;
        VencimientoEstimado = _estado != EstadoGasto.Pagado && vencimientoEstimado;
        ReferenciaComprobante = Normalizar(referenciaComprobante, 120, "La referencia de comprobante");
        Observaciones = Normalizar(observaciones, 1000, "Las observaciones");
        ActualizadoEnUtc = DateTimeOffset.UtcNow;
        ActualizadoPorUsuarioId = usuarioId;
        ActualizarEstado();
    }

    public PagoGasto RegistrarPago(
        Guid medioPagoId,
        decimal importe,
        DateOnly fecha,
        string? referencia,
        string? observacion,
        Guid usuarioId)
    {
        if (Estado is EstadoGasto.Pagado or EstadoGasto.Anulado)
            throw new DomainException("El gasto no admite nuevos pagos.");
        if (importe <= 0 || decimal.Round(importe, 2) > SaldoPendiente)
            throw new DomainException("El importe del pago debe ser positivo y no superar el saldo pendiente.");
        if (fecha < Fecha) throw new DomainException("La fecha del pago no puede ser anterior a la fecha del gasto.");
        if (fecha == default) throw new DomainException("La fecha del pago no es valida.");

        var payment = new PagoGasto(Id, medioPagoId, importe, fecha, referencia, observacion, usuarioId);
        _pagos.Add(payment);
        ActualizadoEnUtc = DateTimeOffset.UtcNow;
        ActualizadoPorUsuarioId = usuarioId;
        ActualizarEstado();
        return payment;
    }

    public ReintegroGasto RegistrarReintegro(
        Guid medioPagoId,
        decimal importe,
        DateOnly fecha,
        string? referencia,
        string? observacion,
        Guid usuarioId)
    {
        if (Estado == EstadoGasto.Anulado) throw new DomainException("El gasto ya esta anulado.");
        if (TotalPagado == 0 || importe <= 0 || decimal.Round(TotalReintegrado + importe, 2) > TotalPagado)
            throw new DomainException("El reintegro debe ser positivo y no superar los pagos realizados.");
        if (fecha < Fecha) throw new DomainException("La fecha del reintegro no puede ser anterior a la fecha del gasto.");
        if (fecha == default) throw new DomainException("La fecha del reintegro no es valida.");

        var refund = new ReintegroGasto(Id, medioPagoId, importe, fecha, referencia, observacion, usuarioId);
        _reintegros.Add(refund);
        ActualizadoEnUtc = DateTimeOffset.UtcNow;
        ActualizadoPorUsuarioId = usuarioId;
        return refund;
    }

    public void Anular(string motivo, Guid usuarioId)
    {
        if (Estado == EstadoGasto.Anulado) throw new DomainException("El gasto ya esta anulado.");
        if (usuarioId == Guid.Empty) throw new DomainException("El usuario responsable no es valido.");
        if (TotalReintegrado != TotalPagado)
            throw new DomainException("Para anular un gasto con pagos, debe reintegrarse el importe abonado completo.");
        MotivoAnulacion = NormalizarRequerido(motivo, 500, "El motivo de anulacion");
        AnuladoEnUtc = DateTimeOffset.UtcNow;
        AnuladoPorUsuarioId = usuarioId;
        Estado = EstadoGasto.Anulado;
        ActualizadoEnUtc = AnuladoEnUtc.Value;
        ActualizadoPorUsuarioId = usuarioId;
    }

    private void ActualizarEstado()
    {
        if (_estado == EstadoGasto.Anulado) return;
        Estado = TotalPagado == ImporteTotal
            ? EstadoGasto.Pagado
            : TotalPagado > 0
                ? EstadoGasto.ParcialmentePagado
                : EstadoGasto.Pendiente;
    }

    private static string NormalizarRequerido(string? value, int max, string field)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > max)
            throw new DomainException($"{field} es obligatorio y no puede superar {max} caracteres.");
        return normalized;
    }

    private static string? Normalizar(string? value, int max, string field)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return null;
        if (normalized.Length > max) throw new DomainException($"{field} no puede superar {max} caracteres.");
        return normalized;
    }
}
