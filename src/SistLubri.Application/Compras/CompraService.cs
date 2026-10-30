using SistLubri.Application.Caja;
using SistLubri.Application.Common.Exceptions;
using SistLubri.Application.Common.Persistence;
using SistLubri.Application.Common.Security;
using SistLubri.Application.Productos;
using SistLubri.Application.Proveedores;
using SistLubri.Domain.Caja;
using SistLubri.Domain.Common;
using SistLubri.Domain.Compras;
using SistLubri.Domain.Productos;
using SistLubri.Domain.Proveedores;

namespace SistLubri.Application.Compras;

public sealed class CompraService(
    ICompraRepository compraRepository,
    IProductoRepository productoRepository,
    IProveedorRepository proveedorRepository,
    ICajaRepository cajaRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
{
    public async Task<CompraDetalleDto> CreateAsync(CrearCompraCommand command, CancellationToken cancellationToken = default)
    {
        var provider = await GetActiveProviderAsync(command.ProveedorId, cancellationToken);
        var lines = await BuildLinesAsync(command.Detalles, cancellationToken);
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var order = new Compra(provider, command.Fecha, command.Observaciones, lines, currentUser.Id);
        compraRepository.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(order);
    }

    public async Task<IReadOnlyList<CompraResumenDto>> SearchAsync(DateOnly? desde, DateOnly? hasta,
        Guid? proveedorId, EstadoRecepcionCompra? recepcion, EstadoPagoCompra? pago,
        CancellationToken cancellationToken = default)
    {
        if ((desde.HasValue && hasta.HasValue && desde > hasta) || proveedorId == Guid.Empty ||
            (recepcion.HasValue && !Enum.IsDefined(recepcion.Value)) || (pago.HasValue && !Enum.IsDefined(pago.Value)))
            throw new DomainException("Los filtros de compra no son validos.");
        var orders = await compraRepository.SearchAsync(desde, hasta, proveedorId, recepcion, pago, cancellationToken);
        return orders.Select(MapSummary).ToArray();
    }

    public async Task<CompraDetalleDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Map(await GetOrderAsync(id, cancellationToken));

    public async Task<CompraDetalleDto> UpdateAsync(Guid id, ActualizarCompraCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var order = await GetOrderAsync(id, cancellationToken);
        ValidateVersion(order, command.Version);
        var provider = await GetActiveProviderAsync(command.ProveedorId, cancellationToken);
        var lines = await BuildLinesAsync(command.Detalles, cancellationToken);
        order.ActualizarDatos(provider, command.Fecha, command.Observaciones, lines, currentUser.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(order);
    }

    public async Task<CompraDetalleDto> RegisterReceiptAsync(Guid id, RegistrarRecepcionCompraCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var order = await GetOrderAsync(id, cancellationToken);
        var products = new Dictionary<Guid, Producto>();
        foreach (var line in command.Lineas)
        {
            var detail = order.Detalles.SingleOrDefault(item => item.Id == line.DetalleCompraId)
                ?? throw new DomainException("La linea indicada no pertenece a la orden.");
            if (!products.ContainsKey(detail.ProductoId))
                products.Add(detail.ProductoId, await GetProductAsync(detail.ProductoId, cancellationToken));
        }

        var receiptLines = command.Lineas.Select(line => new LineaRecepcionInicial(
            line.DetalleCompraId, line.CantidadAceptada, line.CantidadDanada, line.CostoUnitario, line.Observaciones)).ToArray();
        var receipt = order.RegistrarRecepcion(command.Fecha, command.Remito, command.Observaciones,
            receiptLines, products, currentUser.Id);
        compraRepository.Add(receipt);
        foreach (var line in receiptLines.Where(item => item.CantidadAceptada > 0))
        {
            var detail = order.Detalles.Single(item => item.Id == line.DetalleCompraId);
            var movement = products[detail.ProductoId].Movimientos.Last();
            productoRepository.AddMovement(movement);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(order);
    }

    public async Task<CompraDetalleDto> RegisterInvoiceAsync(Guid id, RegistrarFacturaCompraCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var order = await GetOrderAsync(id, cancellationToken);
        ValidateVersion(order, command.Version);
        if (await compraRepository.ExistsInvoiceNumberAsync(order.ProveedorId, command.Numero.Trim(), cancellationToken))
            throw new ConflictException("Ya existe una factura con ese numero para el proveedor.");
        order.RegistrarFactura(command.Numero, command.Fecha, command.FechaVencimiento,
            command.ImporteTotal, command.Observaciones, currentUser.Id);
        var invoice = order.Facturas.Last();
        compraRepository.Add(invoice);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(order);
    }

    public async Task<CompraDetalleDto> RegisterPaymentAsync(Guid id, Guid facturaId,
        RegistrarPagoCompraCommand command, CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var order = await GetOrderAsync(id, cancellationToken);
        ValidateVersion(order, command.Version);
        var method = await cajaRepository.GetMedioPagoAsync(command.MedioPagoId, cancellationToken)
            ?? throw new NotFoundException("No se encontro el medio de pago.");
        if (!method.Activo) throw new DomainException("El medio de pago esta inactivo.");
        if (method.RequiereReferencia && string.IsNullOrWhiteSpace(command.Referencia))
            throw new DomainException("El medio de pago requiere una referencia.");
        var shift = await cajaRepository.GetActualAsync(cancellationToken)
            ?? throw new ConflictException("Debe haber una jornada de caja abierta para registrar el pago.");
        var payment = order.RegistrarPago(facturaId, command.Fecha, method.Id, command.Importe,
            command.Referencia, command.Observacion, currentUser.Id);
        var movement = shift.RegistrarMovimiento(TipoMovimientoCaja.EgresoCompra, false, payment.Importe,
            currentUser.Id, method.Id, referencia: payment.Referencia, motivo: $"Pago de factura {facturaId}",
            afectaEfectivo: method.AfectaEfectivo, facturaCompraId: facturaId, pagoCompraId: payment.Id);
        compraRepository.Add(payment);
        cajaRepository.Add(movement);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(order);
    }

    public async Task<CompraDetalleDto> CloseReceivingAsync(Guid id, string motivo, string version,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var order = await GetOrderAsync(id, cancellationToken);
        ValidateVersion(order, version);
        order.CerrarRecepcion(motivo, currentUser.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(order);
    }

    public async Task<CompraDetalleDto> CancelAsync(Guid id, string motivo, string version,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var order = await GetOrderAsync(id, cancellationToken);
        ValidateVersion(order, version);
        order.Anular(motivo, currentUser.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(order);
    }

    private async Task<IReadOnlyCollection<DetalleCompraInicial>> BuildLinesAsync(
        IReadOnlyList<DetalleCompraCommand> commands, CancellationToken cancellationToken)
    {
        if (commands is null || commands.Count is < 1 or > 100)
            throw new DomainException("La orden debe contener entre 1 y 100 productos.");
        var result = new List<DetalleCompraInicial>(commands.Count);
        foreach (var line in commands)
        {
            var product = await GetProductAsync(line.ProductoId, cancellationToken);
            if (!product.Activo) throw new DomainException($"No se puede agregar el producto inactivo '{product.Nombre}' a la orden.");
            result.Add(new DetalleCompraInicial(product.Id, product.Codigo, product.Nombre,
                product.UnidadMedida.ToString(), line.Cantidad, line.CostoUnitarioEstimado));
        }
        return result;
    }

    private async Task<Proveedor> GetActiveProviderAsync(Guid id, CancellationToken cancellationToken)
    {
        var provider = await proveedorRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("No se encontro el proveedor.");
        if (!provider.Activo) throw new DomainException("No se puede crear una orden con un proveedor inactivo.");
        return provider;
    }

    private async Task<Producto> GetProductAsync(Guid id, CancellationToken cancellationToken)
    {
        return await productoRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("No se encontro uno de los productos de la orden.");
    }

    private async Task<Compra> GetOrderAsync(Guid id, CancellationToken cancellationToken) =>
        await compraRepository.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException("No se encontro la orden de compra.");

    private static void ValidateVersion(Compra order, string encodedVersion)
    {
        byte[] version;
        try { version = Convert.FromBase64String(encodedVersion); }
        catch (FormatException) { throw new DomainException("La version de la orden no tiene un formato valido."); }
        if (!order.RowVersion.SequenceEqual(version))
            throw new ConflictException("La orden fue modificada por otro usuario. Vuelva a consultarla antes de reintentar.");
    }

    private static CompraResumenDto MapSummary(Compra order) => new(order.Id, order.ProveedorId,
        order.Proveedor.RazonSocial, order.Fecha, order.EstadoRecepcion, order.EstadoPago, order.TotalEstimado,
        order.TotalFacturado, order.TotalPagado, order.SaldoPendiente, order.CreadoEnUtc);

    private static CompraDetalleDto Map(Compra order) => new(order.Id, order.ProveedorId,
        order.Proveedor.RazonSocial, order.Fecha, order.Observaciones, order.EstadoRecepcion, order.EstadoPago,
        order.TotalEstimado, order.TotalFacturado, order.TotalPagado, order.SaldoPendiente,
        Convert.ToBase64String(order.RowVersion),
        order.Detalles.Select(item => new DetalleCompraDto(item.Id, item.ProductoId, item.CodigoProducto,
            item.NombreProducto, item.UnidadMedida, item.CantidadSolicitada, item.CantidadAceptada,
            item.CantidadPendiente, item.TotalRecepcionDanada, item.CostoUnitarioEstimado, item.SubtotalEstimado)).ToArray(),
        order.Recepciones.OrderBy(item => item.Fecha).Select(item => new RecepcionCompraDto(item.Id, item.Fecha,
            item.Remito, item.Observaciones, item.UsuarioId, item.CreadaEnUtc,
            item.Lineas.Select(line => new LineaRecepcionCompraDto(line.DetalleCompraId, line.CantidadAceptada,
                line.CantidadDanada, line.CostoUnitario, line.Observaciones)).ToArray())).ToArray(),
        order.Facturas.OrderBy(item => item.Fecha).Select(invoice => new FacturaCompraDto(invoice.Id, invoice.Numero,
            invoice.Fecha, invoice.FechaVencimiento, invoice.ImporteTotal, invoice.TotalPagado, invoice.SaldoPendiente,
            invoice.Pagos.OrderBy(payment => payment.Fecha).Select(payment => new PagoCompraDto(payment.Id, payment.Fecha,
                payment.MedioPagoId, payment.Importe, payment.Referencia, payment.Observacion, payment.UsuarioId)).ToArray())).ToArray(),
        order.CreadoEnUtc, order.CreadoPorUsuarioId);
}
