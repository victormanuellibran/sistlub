using SistLubri.Domain.Compras;

namespace SistLubri.Application.Compras;

public interface ICompraRepository
{
    Task<Compra?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Compra>> SearchAsync(DateOnly? desde, DateOnly? hasta, Guid? proveedorId,
        EstadoRecepcionCompra? recepcion, EstadoPagoCompra? pago, CancellationToken cancellationToken = default);
    Task<bool> ExistsInvoiceNumberAsync(Guid proveedorId, string numero, CancellationToken cancellationToken = default);
    void Add(Compra compra);
    void Add(RecepcionCompra recepcion);
    void Add(FacturaCompra factura);
    void Add(PagoCompra pago);
}
