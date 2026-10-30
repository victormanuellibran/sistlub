using SistLubri.Domain.Common;

namespace SistLubri.Domain.Compras;

public sealed class RecepcionCompra : Entity
{
    private readonly List<LineaRecepcionCompra> _lineas = [];
    private RecepcionCompra() { }
    internal RecepcionCompra(Guid compraId, DateOnly fecha, string? remito, string? observaciones, Guid usuarioId)
    {
        CompraId = compraId;
        Fecha = fecha;
        Remito = Opcional(remito, 120);
        Observaciones = Opcional(observaciones, 1000);
        UsuarioId = usuarioId;
        CreadaEnUtc = DateTimeOffset.UtcNow;
    }
    public Guid CompraId { get; private set; }
    public DateOnly Fecha { get; private set; }
    public string? Remito { get; private set; }
    public string? Observaciones { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTimeOffset CreadaEnUtc { get; private set; }
    public IReadOnlyCollection<LineaRecepcionCompra> Lineas => _lineas.AsReadOnly();
    internal void AgregarLinea(LineaRecepcionCompra line) => _lineas.Add(line);
    private static string? Opcional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > max) throw new DomainException($"El texto no puede superar {max} caracteres.");
        return normalized;
    }
}

public sealed class LineaRecepcionCompra : Entity
{
    private LineaRecepcionCompra() { }
    internal LineaRecepcionCompra(Guid detalleCompraId, decimal aceptada, decimal danada, decimal costo, string? observaciones)
    {
        if (detalleCompraId == Guid.Empty || aceptada < 0 || danada < 0 || aceptada + danada <= 0 || costo < 0)
            throw new DomainException("Los datos de la linea recibida no son validos.");
        DetalleCompraId = detalleCompraId;
        CantidadAceptada = decimal.Round(aceptada, 3);
        CantidadDanada = decimal.Round(danada, 3);
        CostoUnitario = decimal.Round(costo, 2);
        Observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim();
        if (Observaciones?.Length > 500) throw new DomainException("La observacion no puede superar 500 caracteres.");
    }
    public Guid RecepcionCompraId { get; private set; }
    public Guid DetalleCompraId { get; private set; }
    public decimal CantidadAceptada { get; private set; }
    public decimal CantidadDanada { get; private set; }
    public decimal CostoUnitario { get; private set; }
    public string? Observaciones { get; private set; }
}
