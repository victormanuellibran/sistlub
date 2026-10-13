using SistLubri.Domain.Common;

namespace SistLubri.Domain.Productos;

public sealed class MovimientoInventario : Entity
{
    private MovimientoInventario()
    {
    }

    internal MovimientoInventario(
        Guid productoId,
        TipoMovimientoInventario tipo,
        decimal cantidad,
        decimal stockAnterior,
        decimal stockResultante,
        string motivo,
        Guid? referenciaId)
    {
        ProductoId = productoId;
        Tipo = tipo;
        Cantidad = cantidad;
        StockAnterior = stockAnterior;
        StockResultante = stockResultante;
        Motivo = motivo;
        ReferenciaId = referenciaId;
        FechaUtc = DateTimeOffset.UtcNow;
    }

    public Guid ProductoId { get; private init; }

    public Producto Producto { get; private init; } = null!;

    public TipoMovimientoInventario Tipo { get; private init; }

    public decimal Cantidad { get; private init; }

    public decimal StockAnterior { get; private init; }

    public decimal StockResultante { get; private init; }

    public string Motivo { get; private init; } = string.Empty;

    public Guid? ReferenciaId { get; private init; }

    public DateTimeOffset FechaUtc { get; private init; }
}
