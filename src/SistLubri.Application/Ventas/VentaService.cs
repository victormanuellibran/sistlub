using SistLubri.Application.Caja;
using SistLubri.Application.Clientes;
using SistLubri.Application.Common.Exceptions;
using SistLubri.Application.Common.Persistence;
using SistLubri.Application.Common.Security;
using SistLubri.Application.Productos;
using SistLubri.Application.Servicios;
using SistLubri.Domain.Caja;
using SistLubri.Domain.Productos;
using SistLubri.Domain.Servicios;
using SistLubri.Domain.Ventas;

namespace SistLubri.Application.Ventas;

public sealed class VentaService(
    IVentaRepository ventaRepository,
    ICajaRepository cajaRepository,
    IClienteRepository clienteRepository,
    IProductoRepository productoRepository,
    IServicioVehicularRepository servicioRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
{
    public async Task<VentaDetalleDto> CrearVentaAsync(CrearVentaCommand command, CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        if (command.Detalles.Count == 0) throw new Domain.Common.DomainException("La venta debe incluir productos.");
        if (command.Detalles.Select(d => d.ProductoId).Distinct().Count() != command.Detalles.Count)
            throw new Domain.Common.DomainException("Cada producto debe aparecer una sola vez en la venta.");

        var cliente = command.ClienteId.HasValue
            ? await clienteRepository.GetByIdAsync(command.ClienteId.Value, cancellationToken)
                ?? throw new NotFoundException("No se encontro el cliente indicado.")
            : null;
        if (cliente is { Activo: false }) throw new ConflictException("No se puede vender a un cliente inactivo.");

        var sale = Venta.CrearVentaDirecta(
            cliente?.Id,
            cliente is null ? null : $"{cliente.Nombre} {cliente.Apellido}",
            command.ReferenciaFactura,
            currentUser.Id);

        var products = new List<(Producto Product, DetalleVentaCommand Detail)>();
        foreach (var detail in command.Detalles)
        {
            var product = await productoRepository.GetByIdAsync(detail.ProductoId, cancellationToken)
                ?? throw new NotFoundException("No se encontro uno de los productos de la venta.");
            if (!product.Activo) throw new ConflictException($"El producto '{product.Nombre}' esta inactivo.");
            products.Add((product, detail));
            sale.AgregarOActualizarDetalle(
                product.Id, product.Codigo, product.Nombre, product.Presentacion,
                product.UnidadMedida, product.EsFraccionado, detail.Cantidad,
                detail.PrecioUnitario ?? product.PrecioVenta, detail.Descuento, currentUser.Id);
        }
        sale.AplicarDescuentoGeneral(command.DescuentoGeneral, currentUser.Id);

        var jornada = await ObtenerCajaAbiertaAsync(cancellationToken);
        foreach (var (product, detail) in products)
        {
            var movimiento = product.RegistrarMovimiento(
                TipoMovimientoInventario.Salida,
                detail.Cantidad,
                $"Venta directa {sale.Id}",
                sale.Id,
                currentUser.Id);
            productoRepository.AddMovement(movimiento);
        }
        sale.MarcarInventarioDescontado(currentUser.Id);
        await RegistrarPagosAsync(sale, jornada, command.Pagos, currentUser.Id, cancellationToken);
        ventaRepository.Add(sale);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(sale);
    }

    public async Task<VentaDetalleDto> CobrarServicioAsync(Guid servicioId, CobrarServicioCommand command, CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var servicio = await servicioRepository.GetByIdAsync(servicioId, cancellationToken)
            ?? throw new NotFoundException("No se encontro el servicio solicitado.");
        if (servicio.Estado == EstadoServicio.EnCurso)
            throw new ConflictException("No se puede cobrar un servicio que todavia esta en curso.");
        var sale = await ventaRepository.GetByServiceIdAsync(servicioId, cancellationToken);
        var isNewSale = sale is null;
        if (sale is null)
        {
            if (servicio.Estado != EstadoServicio.Finalizado)
                throw new ConflictException("Solo se puede cobrar un servicio finalizado o interrumpido con una venta pendiente.");

            sale = Venta.CrearVentaDeServicio(
                servicio.Id,
                servicio.ClienteId,
                servicio.ClienteNombre,
                servicio.Patente,
                command.ImporteManoObra,
                command.ReferenciaFactura,
                currentUser.Id,
                servicio.Detalles.Select(detail => new DetalleVentaInicial(
                    detail.ProductoId, detail.CodigoProducto, detail.NombreProducto,
                    detail.UnidadMedida.ToString(), detail.UnidadMedida, detail.EsFraccionado,
                    detail.Cantidad, detail.PrecioUnitario)).ToArray());
        }
        else if (sale.Estado != EstadoVenta.PendienteDeCobro)
        {
            throw new ConflictException("La venta asociada al servicio ya no esta pendiente de cobro.");
        }

        if (servicio.Estado == EstadoServicio.Anulado && command.ImporteManoObra != 0)
            throw new ConflictException("Un servicio interrumpido solo puede cobrar los productos efectivamente utilizados.");

        var prices = sale.Detalles.ToDictionary(
            detail => detail.ProductoId,
            detail => (detail.PrecioUnitario, detail.Descuento));
        sale.ActualizarImportesServicio(command.ImporteManoObra, prices, command.DescuentoGeneral, currentUser.Id);
        sale.ActualizarReferenciaFactura(command.ReferenciaFactura, currentUser.Id);

        var jornada = await ObtenerCajaAbiertaAsync(cancellationToken);
        await RegistrarPagosAsync(sale, jornada, command.Pagos, currentUser.Id, cancellationToken);
        if (isNewSale)
            ventaRepository.Add(sale);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(sale);
    }

    public async Task<VentaDetalleDto> EditPendingAsync(Guid id, EditarVentaPendienteCommand command, CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var sale = await ventaRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("No se encontro la venta solicitada.");
        if (sale.Estado != EstadoVenta.PendienteDeCobro) throw new ConflictException("Solo se pueden editar ventas pendientes.");

        if (sale.Origen == OrigenVenta.Productos)
        {
            if (command.Detalles.Select(d => d.ProductoId).Distinct().Count() != command.Detalles.Count)
                throw new Domain.Common.DomainException("Cada producto debe aparecer una sola vez.");
            var requestedIds = command.Detalles.Select(d => d.ProductoId).ToHashSet();
            foreach (var previous in sale.Detalles.ToArray())
            {
                if (requestedIds.Contains(previous.ProductoId)) continue;
                var product = await productoRepository.GetByIdAsync(previous.ProductoId, cancellationToken)
                    ?? throw new NotFoundException("No se encontro un producto previamente vendido.");
                product.RegistrarMovimiento(TipoMovimientoInventario.Entrada, previous.Cantidad,
                    $"Edicion de venta {sale.Id}: devolucion de linea", sale.Id, currentUser.Id);
                sale.EliminarDetalle(previous.ProductoId, currentUser.Id);
            }

            foreach (var detail in command.Detalles)
            {
                var product = await productoRepository.GetByIdAsync(detail.ProductoId, cancellationToken)
                    ?? throw new NotFoundException("No se encontro uno de los productos de la venta.");
                if (!product.Activo) throw new ConflictException($"El producto '{product.Nombre}' esta inactivo.");
                var existing = sale.Detalles.SingleOrDefault(line => line.ProductoId == product.Id);
                var oldQuantity = existing?.Cantidad ?? 0m;
                var delta = detail.Cantidad - oldQuantity;
                if (delta > 0)
                    product.RegistrarMovimiento(TipoMovimientoInventario.Salida, delta, $"Edicion de venta {sale.Id}", sale.Id, currentUser.Id);
                else if (delta < 0)
                    product.RegistrarMovimiento(TipoMovimientoInventario.Entrada, -delta, $"Edicion de venta {sale.Id}", sale.Id, currentUser.Id);
                sale.AgregarOActualizarDetalle(product.Id, product.Codigo, product.Nombre, product.Presentacion,
                    product.UnidadMedida, product.EsFraccionado, detail.Cantidad,
                    detail.PrecioUnitario ?? product.PrecioVenta, detail.Descuento, currentUser.Id);
            }
        }
        else
        {
            if (command.Detalles.Count != sale.Detalles.Count || command.Detalles.Any(line =>
                    sale.Detalles.All(existing => existing.ProductoId != line.ProductoId || existing.Cantidad != line.Cantidad)))
                throw new ConflictException("Los productos y cantidades de un servicio no se modifican desde la venta.");
            if (!command.ImporteManoObra.HasValue) throw new Domain.Common.DomainException("Debe informar el importe de mano de obra.");
            var prices = command.Detalles.ToDictionary(d => d.ProductoId,
                d => (d.PrecioUnitario ?? sale.Detalles.Single(x => x.ProductoId == d.ProductoId).PrecioUnitario, d.Descuento));
            sale.ActualizarImportesServicio(command.ImporteManoObra.Value, prices, command.DescuentoGeneral, currentUser.Id);
        }
        sale.ActualizarReferenciaFactura(command.ReferenciaFactura, currentUser.Id);
        sale.AplicarDescuentoGeneral(command.DescuentoGeneral, currentUser.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(sale);
    }

    public async Task<VentaDetalleDto> CancelAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var sale = await ventaRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("No se encontro la venta solicitada.");
        if (sale.Estado == EstadoVenta.Pagada)
        {
            var day = await cajaRepository.GetActualAsync(cancellationToken)
                ?? throw new ConflictException("Se requiere una caja abierta para registrar la anulacion del cobro.");
            sale.RegistrarAnulacionCobro(sale.Pagos.Select(payment => new RegistrarPagoVenta(
                payment.MedioPagoId, payment.Importe, payment.Referencia, payment.Observacion)).ToArray(), reason, currentUser.Id);
            for (var index = 0; index < sale.Pagos.Count; index++)
            {
                var payment = sale.Pagos.ElementAt(index);
                var reversal = sale.Reintegros.ElementAt(index);
                var refund = day.RegistrarMovimiento(TipoMovimientoCaja.AnulacionVenta, false,
                    payment.Importe, currentUser.Id, payment.MedioPagoId, sale.Id,
                    payment.Referencia, $"Anulacion venta {sale.Id}: {reason}", payment.AfectaEfectivo);
                sale.VincularMovimientoCajaReintegro(index, refund.Id);
            }
        }
        else sale.Anular(reason, currentUser.Id);
        if (sale.Origen == OrigenVenta.Productos && sale.InventarioDescontado)
        {
            foreach (var detail in sale.Detalles)
            {
                var product = await productoRepository.GetByIdAsync(detail.ProductoId, cancellationToken)
                    ?? throw new NotFoundException("No se encontro un producto de la venta.");
                product.RegistrarMovimiento(TipoMovimientoInventario.Entrada, detail.Cantidad,
                    $"Anulacion de venta {sale.Id}: {reason}", sale.Id, currentUser.Id);
            }
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(sale);
    }

    public async Task<VentaDetalleDto> ReturnAsync(Guid id, DevolverVentaCommand command, CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var sale = await ventaRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("No se encontro la venta solicitada.");
        if (sale.Estado is not (EstadoVenta.Pagada or EstadoVenta.DevueltaParcialmente)) throw new ConflictException("Solo se pueden devolver ventas pagadas o parcialmente devueltas.");

        if (sale.Origen == OrigenVenta.Productos)
        {
            if (command.Detalles.Select(d => d.ProductoId).Distinct().Count() != command.Detalles.Count)
                throw new Domain.Common.DomainException("Cada producto devuelto debe aparecer una sola vez.");
            foreach (var returned in command.Detalles)
            {
                var line = sale.Detalles.SingleOrDefault(d => d.ProductoId == returned.ProductoId)
                    ?? throw new Domain.Common.DomainException("El producto no pertenece a esta venta.");
                if (returned.Cantidad <= 0 || returned.Cantidad > line.Cantidad)
                    throw new Domain.Common.DomainException("La cantidad devuelta debe ser positiva y no superar la vendida.");
                line.RegistrarDevolucion(returned.Cantidad);
                if (!returned.ReingresarAlInventario) continue;
                var product = await productoRepository.GetByIdAsync(line.ProductoId, cancellationToken)
                    ?? throw new NotFoundException("No se encontro el producto devuelto.");
                product.RegistrarMovimiento(TipoMovimientoInventario.Entrada, returned.Cantidad,
                    $"Devolucion de venta {sale.Id}: {command.Motivo}", sale.Id, currentUser.Id);
            }
        }
        else if (command.Detalles.Count > 0)
        {
            throw new ConflictException("Los productos consumidos por un servicio no se reintegran automaticamente al inventario.");
        }

        var refunds = command.Reintegros.Select(p => new RegistrarPagoVenta(p.MedioPagoId, p.Importe, p.Referencia, p.Observacion)).ToArray();
        var refundOffset = sale.Reintegros.Count;
        sale.ReemplazarPagosPorReintegro(refunds, command.Motivo, currentUser.Id);
        var day = await cajaRepository.GetActualAsync(cancellationToken)
            ?? throw new ConflictException("Se requiere una caja abierta para registrar un reintegro.");
        for (var index = 0; index < refunds.Length; index++)
        {
            var refund = refunds[index];
            var method = await cajaRepository.GetMedioPagoAsync(refund.MedioPagoId, cancellationToken)
                ?? throw new NotFoundException("No se encontro uno de los medios de reintegro.");
            var movement = day.RegistrarMovimiento(TipoMovimientoCaja.ReintegroVenta, false,
                refund.Importe, currentUser.Id, refund.MedioPagoId, sale.Id, refund.Referencia,
                $"Devolucion venta {sale.Id}: {command.Motivo}", method.AfectaEfectivo);
            sale.VincularMovimientoCajaReintegro(refundOffset + index, movement.Id);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(sale);
    }

    public async Task<VentaDetalleDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await ventaRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("No se encontro la venta solicitada.");
        return Map(sale);
    }

    public async Task<IReadOnlyList<VentaDetalleDto>> SearchAsync(DateTimeOffset? desde, DateTimeOffset? hasta, Guid? clienteId, EstadoVenta? estado, CancellationToken cancellationToken = default)
    {
        var sales = await ventaRepository.SearchAsync(desde, hasta, clienteId, estado, cancellationToken);
        return sales.Select(Map).ToArray();
    }

    private async Task<JornadaCaja> ObtenerCajaAbiertaAsync(CancellationToken cancellationToken)
    {
        return await cajaRepository.GetActualAsync(cancellationToken)
            ?? throw new ConflictException("No hay una jornada de caja abierta.");
    }

    private async Task RegistrarPagosAsync(Venta sale, JornadaCaja jornada, IReadOnlyList<PagoVentaCommand> payments, Guid userId, CancellationToken cancellationToken)
    {
        var validated = new List<RegistrarPagoVenta>(payments.Count);
        var paymentMethods = new List<MedioPago>(payments.Count);
        foreach (var payment in payments)
        {
            var method = await cajaRepository.GetMedioPagoAsync(payment.MedioPagoId, cancellationToken)
                ?? throw new NotFoundException("No se encontro uno de los medios de pago.");
            method.ValidarCobro(payment.Referencia);
            paymentMethods.Add(method);
            validated.Add(new RegistrarPagoVenta(payment.MedioPagoId, payment.Importe, payment.Referencia, payment.Observacion));
        }

        sale.RegistrarCobro(validated, userId);
        sale.EstablecerNombresMediosPago(paymentMethods.Select(method => method.Nombre).ToArray());
        sale.EstablecerAfectacionEfectivoPagos(paymentMethods.Select(method => method.AfectaEfectivo).ToArray());
        for (var index = 0; index < validated.Count; index++)
        {
            var payment = validated[index];
            var method = paymentMethods[index];
            var cashMovement = jornada.RegistrarMovimiento(
                TipoMovimientoCaja.CobroVenta,
                true,
                payment.Importe,
                userId,
                payment.MedioPagoId,
                sale.Id,
                payment.Referencia,
                method.Nombre,
                method.AfectaEfectivo);
            sale.VincularMovimientoCajaPago(index, cashMovement.Id);
        }
    }

    private static VentaDetalleDto Map(Venta sale) => new(
        sale.Id, sale.Origen, sale.Estado, sale.ClienteId, sale.ClienteNombre, sale.Patente,
        sale.ServicioId, sale.ImporteManoObra, sale.TotalBruto, sale.DescuentoTotal, sale.Total,
        sale.InventarioDescontado, sale.ReferenciaFacturaExterna, sale.MotivoAnulacion, sale.MotivoDevolucion, sale.CreadaEnUtc,
        sale.CreadaPorUsuarioId, sale.ActualizadaEnUtc, sale.ActualizadaPorUsuarioId,
        sale.Detalles.Select(detail => new DetalleVentaDto(
            detail.ProductoId, detail.CodigoProducto, detail.NombreProducto, detail.Presentacion,
            detail.Cantidad, detail.PrecioUnitario, detail.Descuento, detail.Subtotal)).ToArray(),
        sale.Pagos.Select(payment => new PagoVentaDto(
            payment.Id, payment.MedioPagoId, payment.NombreMedioPago, payment.Importe, payment.Referencia,
            payment.Observacion, payment.UsuarioId, payment.FechaUtc, payment.MovimientoCajaId)).ToArray(),
        sale.Reintegros.Select(payment => new PagoVentaDto(payment.Id, payment.MedioPagoId, payment.NombreMedioPago,
            payment.Importe, payment.Referencia, payment.Observacion, payment.UsuarioId, payment.FechaUtc,
            payment.MovimientoCajaId)).ToArray());
}
