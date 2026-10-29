namespace SistLubri.Domain.Ventas;

public sealed record RegistrarPagoVenta(
    Guid MedioPagoId,
    decimal Importe,
    string? Referencia = null,
    string? Observacion = null);
