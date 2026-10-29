using SistLubri.Domain.Caja;
using SistLubri.Domain.Common;

namespace SistLubri.Domain.Gastos;

public sealed class PagoGasto : Entity
{
    private PagoGasto() { }

    internal PagoGasto(
        Guid gastoId,
        Guid medioPagoId,
        decimal importe,
        DateOnly fecha,
        string? referencia,
        string? observacion,
        Guid usuarioId)
    {
        if (gastoId == Guid.Empty || medioPagoId == Guid.Empty || usuarioId == Guid.Empty || importe <= 0)
            throw new DomainException("El pago debe tener gasto, medio de pago, usuario e importe validos.");

        GastoId = gastoId;
        MedioPagoId = medioPagoId;
        Importe = decimal.Round(importe, 2);
        Fecha = fecha;
        Referencia = Normalizar(referencia, 120, "La referencia");
        Observacion = Normalizar(observacion, 500, "La observacion");
        UsuarioId = usuarioId;
        CreadoEnUtc = DateTimeOffset.UtcNow;
    }

    public Guid GastoId { get; private init; }
    public Guid MedioPagoId { get; private init; }
    public decimal Importe { get; private init; }
    public DateOnly Fecha { get; private init; }
    public string? Referencia { get; private init; }
    public string? Observacion { get; private init; }
    public Guid UsuarioId { get; private init; }
    public DateTimeOffset CreadoEnUtc { get; private init; }
    public Guid? MovimientoCajaId => MovimientoCaja?.Id;
    public MovimientoCaja? MovimientoCaja { get; private set; }

    public void VincularMovimientoCaja(MovimientoCaja movimiento)
    {
        if (movimiento.GastoId != GastoId || movimiento.PagoGastoId != Id || MovimientoCaja is not null)
            throw new DomainException("El movimiento de caja no corresponde a este pago o ya fue vinculado.");
        MovimientoCaja = movimiento;
    }

    private static string? Normalizar(string? value, int max, string field)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return null;
        if (normalized.Length > max) throw new DomainException($"{field} no puede superar {max} caracteres.");
        return normalized;
    }
}
