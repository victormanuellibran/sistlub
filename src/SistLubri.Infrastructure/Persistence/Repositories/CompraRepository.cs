using Microsoft.EntityFrameworkCore;
using SistLubri.Application.Compras;
using SistLubri.Domain.Compras;

namespace SistLubri.Infrastructure.Persistence.Repositories;

internal sealed class CompraRepository(SistLubriDbContext dbContext) : ICompraRepository
{
    public Task<Compra?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Compras
            .Include(item => item.Proveedor)
            .Include(item => item.Detalles)
            .Include(item => item.Recepciones).ThenInclude(item => item.Lineas)
            .Include(item => item.Facturas).ThenInclude(item => item.Pagos)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Compra>> SearchAsync(DateOnly? desde, DateOnly? hasta, Guid? proveedorId,
        EstadoRecepcionCompra? recepcion, EstadoPagoCompra? pago, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Compras.AsNoTracking().Include(item => item.Proveedor)
            .Include(item => item.Detalles).Include(item => item.Facturas).ThenInclude(item => item.Pagos)
            .AsQueryable();
        if (desde.HasValue) query = query.Where(item => item.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(item => item.Fecha <= hasta.Value);
        if (proveedorId.HasValue) query = query.Where(item => item.ProveedorId == proveedorId.Value);
        var items = await query.OrderByDescending(item => item.Fecha).ThenByDescending(item => item.CreadoEnUtc)
            .Take(200).ToListAsync(cancellationToken);
        return items.Where(item => (!recepcion.HasValue || item.EstadoRecepcion == recepcion.Value) &&
            (!pago.HasValue || item.EstadoPago == pago.Value)).ToArray();
    }

    public Task<bool> ExistsInvoiceNumberAsync(Guid proveedorId, string numero, CancellationToken cancellationToken = default) =>
        dbContext.FacturasCompra.AnyAsync(invoice => invoice.Compra.ProveedorId == proveedorId &&
            invoice.Numero.ToLower() == numero.Trim().ToLower(), cancellationToken);

    public void Add(Compra compra) => dbContext.Compras.Add(compra);
    public void Add(RecepcionCompra recepcion) => dbContext.RecepcionesCompra.Add(recepcion);
    public void Add(FacturaCompra factura) => dbContext.FacturasCompra.Add(factura);
    public void Add(PagoCompra pago) => dbContext.PagosCompra.Add(pago);
}
