using SistLubri.Domain.Common;

namespace SistLubri.Domain.Ventas;

public sealed class ReintegroVenta : Entity
{
    private ReintegroVenta() { }

    internal ReintegroVenta(Guid ventaId, Guid medioPagoId, string nombreMedioPago, decimal importe,
        string? referencia, string? observacion, Guid usuarioId, bool afectaEfectivo, bool esAnulacion)
    {
        if (ventaId == Guid.Empty || medioPagoId == Guid.Empty || usuarioId == Guid.Empty || importe <= 0)
            throw new DomainException("Los datos del reintegro no son validos.");
        VentaId = ventaId;
        MedioPagoId = medioPagoId;
        NombreMedioPago = nombreMedioPago;
        Importe = decimal.Round(importe, 2);
        Referencia = NormalizarOpcional(referencia, 120);
        Observacion = NormalizarOpcional(observacion, 500);
        UsuarioId = usuarioId;
        FechaUtc = DateTimeOffset.UtcNow;
        AfectaEfectivo = afectaEfectivo;
        EsAnulacion = esAnulacion;
    }

    public Guid VentaId { get; private set; }
    public Venta Venta { get; private init; } = null!;
    public Guid MedioPagoId { get; private set; }
    public string NombreMedioPago { get; private set; } = string.Empty;
    public decimal Importe { get; private set; }
    public string? Referencia { get; private set; }
    public string? Observacion { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTimeOffset FechaUtc { get; private set; }
    public bool AfectaEfectivo { get; private set; }
    public bool EsAnulacion { get; private set; }
    public Guid? MovimientoCajaId { get; private set; }

    internal void VincularMovimientoCaja(Guid movimientoCajaId)
    {
        if (movimientoCajaId == Guid.Empty) throw new DomainException("El movimiento de caja no es valido.");
        MovimientoCajaId = movimientoCajaId;
    }

    private static string? NormalizarOpcional(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return null;
        if (normalized.Length > maxLength) throw new DomainException($"El texto no puede superar los {maxLength} caracteres.");
        return normalized;
    }
}
