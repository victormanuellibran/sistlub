using SistLubri.Domain.Common;

namespace SistLubri.Domain.Compras;

public sealed class FacturaCompra : Entity
{
    private readonly List<PagoCompra> _pagos = [];
    private FacturaCompra() { }
    internal FacturaCompra(Guid compraId, string numero, DateOnly fecha, DateOnly? vencimiento,
        decimal importe, string? observaciones, Guid usuarioId)
    {
        CompraId = compraId;
        Numero = numero.Trim();
        Fecha = fecha;
        FechaVencimiento = vencimiento;
        ImporteTotal = decimal.Round(importe, 2);
        Observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim();
        CreadoPorUsuarioId = usuarioId;
        CreadaEnUtc = DateTimeOffset.UtcNow;
    }
    public Guid CompraId { get; private set; }
    public Compra Compra { get; private set; } = null!;
    public string Numero { get; private set; } = string.Empty;
    public DateOnly Fecha { get; private set; }
    public DateOnly? FechaVencimiento { get; private set; }
    public decimal ImporteTotal { get; private set; }
    public string? Observaciones { get; private set; }
    public Guid CreadoPorUsuarioId { get; private set; }
    public DateTimeOffset CreadaEnUtc { get; private set; }
    public IReadOnlyCollection<PagoCompra> Pagos => _pagos.AsReadOnly();
    public decimal TotalPagado => decimal.Round(_pagos.Sum(item => item.Importe), 2);
    public decimal SaldoPendiente => decimal.Round(ImporteTotal - TotalPagado, 2);
    internal PagoCompra RegistrarPago(DateOnly fecha, Guid medioPagoId, decimal importe, string? referencia,
        string? observacion, Guid usuarioId)
    {
        if (fecha < Fecha || medioPagoId == Guid.Empty || importe <= 0 || usuarioId == Guid.Empty)
            throw new DomainException("La fecha, medio de pago, importe o usuario no son validos.");
        var amount = decimal.Round(importe, 2);
        if (amount > SaldoPendiente) throw new DomainException("El pago supera el saldo pendiente de la factura.");
        var payment = new PagoCompra(Id, fecha, medioPagoId, amount, referencia, observacion, usuarioId);
        _pagos.Add(payment);
        return payment;
    }
}

public sealed class PagoCompra : Entity
{
    private PagoCompra() { }
    internal PagoCompra(Guid facturaId, DateOnly fecha, Guid medioPagoId, decimal importe,
        string? referencia, string? observacion, Guid usuarioId)
    {
        FacturaCompraId = facturaId;
        Fecha = fecha;
        MedioPagoId = medioPagoId;
        Importe = importe;
        Referencia = Texto(referencia, 120);
        Observacion = Texto(observacion, 500);
        UsuarioId = usuarioId;
        CreadoEnUtc = DateTimeOffset.UtcNow;
    }
    public Guid FacturaCompraId { get; private set; }
    public DateOnly Fecha { get; private set; }
    public Guid MedioPagoId { get; private set; }
    public decimal Importe { get; private set; }
    public string? Referencia { get; private set; }
    public string? Observacion { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTimeOffset CreadoEnUtc { get; private set; }
    private static string? Texto(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > max) throw new DomainException($"El texto no puede superar {max} caracteres.");
        return normalized;
    }
}
