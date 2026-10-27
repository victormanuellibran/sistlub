using SistLubri.Domain.Common;

namespace SistLubri.Domain.Caja;

public sealed class MovimientoCaja : Entity
{
    private MovimientoCaja() { }

    internal MovimientoCaja(
        Guid jornadaCajaId,
        TipoMovimientoCaja tipo,
        bool esIngreso,
        decimal importe,
        Guid usuarioId,
        Guid? medioPagoId,
        Guid? ventaId,
        string? referencia,
        string? motivo,
        bool afectaEfectivo,
        Guid? pagoVentaId = null)
    {
        if (jornadaCajaId == Guid.Empty || usuarioId == Guid.Empty || importe <= 0)
        {
            throw new DomainException("El movimiento debe tener jornada, usuario e importe validos.");
        }

        JornadaCajaId = jornadaCajaId;
        Tipo = tipo;
        EsIngreso = esIngreso;
        AfectaEfectivo = afectaEfectivo;
        Importe = decimal.Round(importe, 2);
        UsuarioId = usuarioId;
        MedioPagoId = medioPagoId;
        VentaId = ventaId;
        PagoVentaId = pagoVentaId;
        Referencia = Normalizar(referencia, 120);
        Motivo = Normalizar(motivo, 500);
        FechaUtc = DateTimeOffset.UtcNow;
    }

    public Guid JornadaCajaId { get; private set; }
    public JornadaCaja JornadaCaja { get; private init; } = null!;
    public TipoMovimientoCaja Tipo { get; private init; }
    public bool EsIngreso { get; private init; }
    public bool AfectaEfectivo { get; private init; }
    public decimal Importe { get; private init; }
    public Guid UsuarioId { get; private init; }
    public Guid? MedioPagoId { get; private init; }
    public Guid? VentaId { get; private init; }
    public Guid? PagoVentaId { get; private init; }
    public string? Referencia { get; private init; }
    public string? Motivo { get; private init; }
    public DateTimeOffset FechaUtc { get; private init; }

    private static string? Normalizar(string? value, int max)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return null;
        if (normalized.Length > max) throw new DomainException($"El texto no puede superar {max} caracteres.");
        return normalized;
    }
}
