using SistLubri.Domain.Common;

namespace SistLubri.Domain.Ventas;

public sealed class PagoVenta : Entity
{
    private PagoVenta()
    {
    }

    internal PagoVenta(
        Guid ventaId,
        Guid medioPagoId,
        decimal importe,
        string? referencia,
        string? observacion,
        Guid usuarioId)
    {
        if (ventaId == Guid.Empty || medioPagoId == Guid.Empty || usuarioId == Guid.Empty)
        {
            throw new DomainException("El pago debe indicar una venta, un medio de pago y un usuario validos.");
        }

        if (importe <= 0)
        {
            throw new DomainException("El importe del pago debe ser mayor que cero.");
        }

        VentaId = ventaId;
        MedioPagoId = medioPagoId;
        Importe = decimal.Round(importe, 2);
        Referencia = NormalizarOpcional(referencia, 120, "La referencia");
        Observacion = NormalizarOpcional(observacion, 500, "La observacion");
        UsuarioId = usuarioId;
        FechaUtc = DateTimeOffset.UtcNow;
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

    public Guid? MovimientoCajaId { get; private set; }

    public bool EsReintegro { get; private set; }

    public bool AfectaEfectivo { get; private set; }

    public bool EsAnulacion { get; private set; }

    public string? TipoOperacion { get; private set; }

    internal void VincularMovimientoCaja(Guid movimientoCajaId)
    {
        if (movimientoCajaId == Guid.Empty) throw new DomainException("El movimiento de caja no es valido.");
        MovimientoCajaId = movimientoCajaId;
    }

    internal void MarcarReintegro(string nombreMedioPago)
    {
        EsReintegro = true;
        NombreMedioPago = $"Reintegro - {nombreMedioPago}";
    }

    internal void MarcarAnulacion()
    {
        EsAnulacion = true;
        TipoOperacion = "Anulacion";
    }

    internal void EstablecerNombreMedioPago(string nombre)
    {
        NombreMedioPago = nombre;
    }

    internal void EstablecerAfectaEfectivo(bool afectaEfectivo) => AfectaEfectivo = afectaEfectivo;

    internal void ConvertirEnReintegro(Guid medioPagoId, decimal importe, string? referencia, string? observacion, Guid usuarioId)
    {
        if (medioPagoId == Guid.Empty || importe <= 0 || usuarioId == Guid.Empty)
            throw new DomainException("Los datos del reintegro no son validos.");
        MedioPagoId = medioPagoId;
        Importe = decimal.Round(importe, 2);
        Referencia = NormalizarOpcional(referencia, 120, "La referencia");
        Observacion = NormalizarOpcional(observacion, 500, "La observacion");
        NombreMedioPago = $"Reintegro - {NombreMedioPago}";
        TipoOperacion = "Devolucion";
        UsuarioId = usuarioId;
        FechaUtc = DateTimeOffset.UtcNow;
    }

    internal void RegistrarAnulacion(Guid medioPagoId, string? referencia, string? observacion, Guid usuarioId)
    {
        MedioPagoId = medioPagoId;
        Referencia = NormalizarOpcional(referencia, 120, "La referencia");
        Observacion = NormalizarOpcional(observacion, 500, "La observacion");
        NombreMedioPago = $"Anulacion - {NombreMedioPago}";
        TipoOperacion = "Anulacion";
        UsuarioId = usuarioId;
        FechaUtc = DateTimeOffset.UtcNow;
    }

    private static string? NormalizarOpcional(string? value, int maxLength, string field)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{field} no puede superar los {maxLength} caracteres.");
        }

        return normalized;
    }
}
