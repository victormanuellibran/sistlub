using SistLubri.Domain.Common;
using SistLubri.Domain.Productos;

namespace SistLubri.Domain.Ventas;

public sealed class Venta : Entity
{
    private readonly List<DetalleVenta> _detalles = [];
    private readonly List<PagoVenta> _pagos = [];
    private readonly List<ReintegroVenta> _reintegros = [];

    private Venta()
    {
    }

    private Venta(
        OrigenVenta origen,
        Guid? clienteId,
        string? clienteNombre,
        string? patente,
        Guid? servicioId,
        decimal importeManoObra,
        string? referenciaFacturaExterna,
        Guid usuarioId,
        Guid? ventaId = null)
    {
        if (!Enum.IsDefined(origen))
        {
            throw new DomainException("El origen de la venta no es valido.");
        }

        ValidarUsuario(usuarioId);
        if (ventaId.HasValue) Id = ventaId.Value;
        if (clienteId == Guid.Empty)
        {
            throw new DomainException("El identificador del cliente no es valido.");
        }

        if (origen == OrigenVenta.ServicioVehicular &&
            (!servicioId.HasValue || servicioId == Guid.Empty || !clienteId.HasValue))
        {
            throw new DomainException("La venta de un servicio debe estar vinculada a un servicio y un cliente validos.");
        }

        if (origen == OrigenVenta.Productos && (servicioId.HasValue || importeManoObra != 0))
        {
            throw new DomainException("Una venta directa no puede incluir un servicio vehicular ni mano de obra.");
        }

        if (importeManoObra < 0)
        {
            throw new DomainException("El importe de mano de obra no puede ser negativo.");
        }

        if (referenciaFacturaExterna?.Trim().Length > 120)
        {
            throw new DomainException("La referencia de factura no puede superar 120 caracteres.");
        }

        Origen = origen;
        ClienteId = clienteId;
        ClienteNombre = clienteId.HasValue ? Normalizar(clienteNombre, 201, "El nombre del cliente") : null;
        Patente = origen == OrigenVenta.ServicioVehicular
            ? Normalizar(patente, 10, "La patente")
            : NormalizarOpcional(patente, 10, "La patente");
        ServicioId = servicioId;
        ImporteManoObra = decimal.Round(importeManoObra, 2);
        ReferenciaFacturaExterna = NormalizarOpcional(referenciaFacturaExterna, 120, "La referencia de factura");
        Estado = EstadoVenta.PendienteDeCobro;
        InventarioDescontado = origen == OrigenVenta.ServicioVehicular;
        CreadaEnUtc = DateTimeOffset.UtcNow;
        ActualizadaEnUtc = CreadaEnUtc;
        CreadaPorUsuarioId = usuarioId;
        ActualizadaPorUsuarioId = usuarioId;
    }

    public static Venta CrearVentaDirecta(
        Guid? clienteId,
        string? clienteNombre,
        string? referenciaFacturaExterna,
        Guid usuarioId,
        Guid? ventaId = null) => new(
            OrigenVenta.Productos,
            clienteId,
            clienteNombre,
            null,
            null,
            0,
            referenciaFacturaExterna,
            usuarioId,
            ventaId);

    public static Venta CrearVentaDeServicio(
        Guid servicioId,
        Guid clienteId,
        string clienteNombre,
        string patente,
        decimal importeManoObra,
        string? referenciaFacturaExterna,
        Guid usuarioId,
        IReadOnlyCollection<DetalleVentaInicial> detalles,
        Guid? ventaId = null)
    {
        ArgumentNullException.ThrowIfNull(detalles);
        var venta = new Venta(
            OrigenVenta.ServicioVehicular,
            clienteId,
            clienteNombre,
            patente,
            servicioId,
            importeManoObra,
            referenciaFacturaExterna,
            usuarioId,
            ventaId);

        if (detalles.Select(detalle => detalle.ProductoId).Distinct().Count() != detalles.Count)
        {
            throw new DomainException("Cada producto debe aparecer una sola vez en la venta.");
        }

        foreach (var detalle in detalles)
        {
            venta._detalles.Add(new DetalleVenta(
                venta.Id,
                detalle.ProductoId,
                detalle.CodigoProducto,
                detalle.NombreProducto,
                detalle.Presentacion,
                detalle.UnidadMedida,
                detalle.EsFraccionado,
                detalle.Cantidad,
                detalle.PrecioUnitario,
                detalle.Descuento,
                true));
        }

        return venta;
    }

    public OrigenVenta Origen { get; private init; }

    public Guid? ClienteId { get; private init; }

    public string? ClienteNombre { get; private init; }

    public string? Patente { get; private init; }

    public Guid? ServicioId { get; private init; }

    public decimal ImporteManoObra { get; private set; }

    public string? ReferenciaFacturaExterna { get; private set; }

    public EstadoVenta Estado { get; private set; }

    public bool InventarioDescontado { get; private set; }

    public DateTimeOffset CreadaEnUtc { get; private init; }

    public DateTimeOffset ActualizadaEnUtc { get; private set; }

    public Guid CreadaPorUsuarioId { get; private init; }

    public Guid ActualizadaPorUsuarioId { get; private set; }

    public DateTimeOffset? PagadaEnUtc { get; private set; }

    public Guid? PagadaPorUsuarioId { get; private set; }

    public DateTimeOffset? AnuladaEnUtc { get; private set; }

    public Guid? AnuladaPorUsuarioId { get; private set; }

    public string? MotivoAnulacion { get; private set; }

    public decimal DescuentoGeneral { get; private set; }

    public decimal TotalBruto => decimal.Round(_detalles.Sum(detalle => detalle.SubtotalBruto) + ImporteManoObra, 2);

    public decimal DescuentoTotal => decimal.Round(_detalles.Sum(detalle => detalle.Descuento) + DescuentoGeneral, 2);

    public decimal Total => decimal.Round(TotalBruto - DescuentoTotal, 2);

    public IReadOnlyCollection<DetalleVenta> Detalles => _detalles.AsReadOnly();

    public IReadOnlyCollection<PagoVenta> Pagos => _pagos.AsReadOnly();

    public IReadOnlyCollection<ReintegroVenta> Reintegros => _reintegros.AsReadOnly();

    public void AgregarOActualizarDetalle(
        Guid productoId,
        string codigoProducto,
        string nombreProducto,
        string presentacion,
        UnidadMedidaProducto unidadMedida,
        bool esFraccionado,
        decimal cantidad,
        decimal precioUnitario,
        decimal descuento,
        Guid usuarioId)
    {
        ValidarPendiente(usuarioId);
        if (Origen != OrigenVenta.Productos)
        {
            throw new DomainException("Los productos de un servicio no se pueden modificar desde la venta.");
        }

        var existing = _detalles.SingleOrDefault(detalle => detalle.ProductoId == productoId);
        ValidarDescuentoConDetalle(productoId, cantidad, precioUnitario, descuento);
        if (existing is not null)
        {
            existing.ActualizarCantidadYPrecios(cantidad, precioUnitario, descuento);
        }
        else
        {
            _detalles.Add(new DetalleVenta(
                Id,
                productoId,
                codigoProducto,
                nombreProducto,
                presentacion,
                unidadMedida,
                esFraccionado,
                cantidad,
                precioUnitario,
                descuento,
                false));
        }

        ActualizarAuditoria(usuarioId);
    }

    public void ActualizarImportesServicio(
        decimal importeManoObra,
        IReadOnlyDictionary<Guid, (decimal PrecioUnitario, decimal Descuento)> importesProductos,
        decimal descuentoGeneral,
        Guid usuarioId)
    {
        ValidarPendiente(usuarioId);
        if (Origen != OrigenVenta.ServicioVehicular)
        {
            throw new DomainException("Esta operacion solo corresponde a ventas originadas por un servicio.");
        }

        ArgumentNullException.ThrowIfNull(importesProductos);
        if (importeManoObra < 0 || descuentoGeneral < 0)
        {
            throw new DomainException("La mano de obra y el descuento no pueden ser negativos.");
        }

        if (importesProductos.Count != _detalles.Count ||
            _detalles.Any(detalle => !importesProductos.ContainsKey(detalle.ProductoId)))
        {
            throw new DomainException("La venta debe conservar los productos y cantidades del servicio asociado.");
        }

        var manoObraNormalizada = decimal.Round(importeManoObra, 2);
        var descuentoNormalizado = decimal.Round(descuentoGeneral, 2);
        var brutoProductos = 0m;
        var descuentosProductos = 0m;
        foreach (var detalle in _detalles)
        {
            var importes = importesProductos[detalle.ProductoId];
            var precio = decimal.Round(importes.PrecioUnitario, 2);
            var descuento = decimal.Round(importes.Descuento, 2);
            if (precio < 0 || descuento < 0 || descuento > decimal.Round(detalle.Cantidad * precio, 2))
            {
                throw new DomainException("El precio o descuento de un producto de servicio no es valido.");
            }

            brutoProductos += decimal.Round(detalle.Cantidad * precio, 2);
            descuentosProductos += descuento;
        }

        var disponibleParaDescuento = decimal.Round(manoObraNormalizada + brutoProductos - descuentosProductos, 2);
        if (descuentoNormalizado > disponibleParaDescuento)
        {
            throw new DomainException("El descuento general no puede superar el importe de la venta.");
        }

        foreach (var detalle in _detalles)
        {
            var importes = importesProductos[detalle.ProductoId];
            detalle.ActualizarImportesComerciales(importes.PrecioUnitario, importes.Descuento);
        }

        ImporteManoObra = manoObraNormalizada;
        DescuentoGeneral = descuentoNormalizado;
        ActualizarAuditoria(usuarioId);
    }

    public void EliminarDetalle(Guid productoId, Guid usuarioId)
    {
        ValidarPendiente(usuarioId);
        if (Origen != OrigenVenta.Productos)
        {
            throw new DomainException("Los productos utilizados en un servicio no se pueden quitar desde la venta.");
        }

        var detalle = _detalles.SingleOrDefault(item => item.ProductoId == productoId)
            ?? throw new DomainException("El producto no forma parte de esta venta.");
        var grossWithoutDetail = decimal.Round(TotalBruto - detalle.SubtotalBruto, 2);
        var lineDiscountsWithoutDetail = decimal.Round(_detalles.Sum(item => item.Descuento) - detalle.Descuento, 2);
        if (DescuentoGeneral > grossWithoutDetail - lineDiscountsWithoutDetail)
        {
            throw new DomainException("No se puede quitar el producto porque el descuento general superaria el nuevo total.");
        }

        _detalles.Remove(detalle);
        ActualizarAuditoria(usuarioId);
    }

    public void AplicarDescuentoGeneral(decimal descuento, Guid usuarioId)
    {
        ValidarPendiente(usuarioId);
        if (descuento < 0)
        {
            throw new DomainException("El descuento no puede ser negativo.");
        }

        var normalizedDiscount = decimal.Round(descuento, 2);
        var maximumDiscount = decimal.Round(TotalBruto - _detalles.Sum(detalle => detalle.Descuento), 2);
        if (normalizedDiscount > maximumDiscount)
        {
            throw new DomainException("El descuento general no puede superar el importe de la venta.");
        }

        DescuentoGeneral = normalizedDiscount;
        ActualizarAuditoria(usuarioId);
    }

    public void ActualizarReferenciaFactura(string? referencia, Guid usuarioId)
    {
        ValidarPendiente(usuarioId);
        ReferenciaFacturaExterna = NormalizarOpcional(referencia, 120, "La referencia de factura");
        ActualizarAuditoria(usuarioId);
    }

    public void MarcarInventarioDescontado(Guid usuarioId)
    {
        ValidarPendiente(usuarioId);
        if (Origen != OrigenVenta.Productos)
        {
            throw new DomainException("La venta de un servicio no debe volver a descontar inventario.");
        }

        if (InventarioDescontado)
        {
            throw new DomainException("El inventario de esta venta ya fue descontado.");
        }

        InventarioDescontado = true;
        ActualizarAuditoria(usuarioId);
    }

    public void RegistrarCobro(IReadOnlyCollection<RegistrarPagoVenta> pagos, Guid usuarioId)
    {
        ValidarPendiente(usuarioId);
        ArgumentNullException.ThrowIfNull(pagos);
        if (_detalles.Count == 0 && ImporteManoObra == 0)
        {
            throw new DomainException("La venta debe incluir al menos un producto o un importe de mano de obra.");
        }

        if (!InventarioDescontado)
        {
            throw new DomainException("Debe descontarse el inventario antes de cobrar una venta directa.");
        }

        if (pagos.Count == 0 || pagos.Any(pago => pago.MedioPagoId == Guid.Empty || pago.Importe <= 0))
        {
            throw new DomainException("Debe registrar al menos un pago valido.");
        }

        if (Total <= 0)
        {
            throw new DomainException("El total de la venta debe ser mayor que cero para registrar un cobro.");
        }

        var importePagado = decimal.Round(pagos.Sum(pago => decimal.Round(pago.Importe, 2)), 2);
        if (importePagado != Total)
        {
            throw new DomainException("La suma de los medios de pago debe coincidir con el total de la venta.");
        }

        var pagosValidados = pagos.Select(pago => new PagoVenta(
                Id,
                pago.MedioPagoId,
                pago.Importe,
                pago.Referencia,
                pago.Observacion,
                usuarioId))
            .ToArray();

        _pagos.AddRange(pagosValidados);

        Estado = EstadoVenta.Pagada;
        PagadaEnUtc = DateTimeOffset.UtcNow;
        PagadaPorUsuarioId = usuarioId;
        ActualizarAuditoria(usuarioId);
    }

    public void EstablecerNombresMediosPago(IReadOnlyList<string> nombres)
    {
        if (nombres.Count != _pagos.Count)
        {
            throw new DomainException("Los nombres de medios de pago no coinciden con los pagos registrados.");
        }

        for (var index = 0; index < nombres.Count; index++)
        {
            _pagos[index].EstablecerNombreMedioPago(nombres[index]);
        }
    }

    public void EstablecerAfectacionEfectivoPagos(IReadOnlyList<bool> afectaEfectivo)
    {
        if (afectaEfectivo.Count != _pagos.Count)
            throw new DomainException("La configuracion de los medios de pago no coincide con los pagos registrados.");
        for (var index = 0; index < afectaEfectivo.Count; index++)
            _pagos[index].EstablecerAfectaEfectivo(afectaEfectivo[index]);
    }

    public void VincularMovimientoCajaPago(int indice, Guid movimientoCajaId)
    {
        if (indice < 0 || indice >= _pagos.Count) throw new DomainException("No existe el pago indicado.");
        _pagos[indice].VincularMovimientoCaja(movimientoCajaId);
    }

    public void VincularMovimientoCajaReintegro(int indice, Guid movimientoCajaId)
    {
        if (indice < 0 || indice >= _reintegros.Count) throw new DomainException("No existe el reintegro indicado.");
        _reintegros[indice].VincularMovimientoCaja(movimientoCajaId);
    }

    public void EliminarPagoYRevertirCobro(Guid usuarioId)
    {
        ValidarUsuario(usuarioId);
        if (Estado != EstadoVenta.Pagada)
        {
            throw new DomainException("Solo se puede revertir el cobro de una venta pagada.");
        }

        Estado = EstadoVenta.PendienteDeCobro;
        PagadaEnUtc = null;
        PagadaPorUsuarioId = null;
        ActualizarAuditoria(usuarioId);
    }

    public void RegistrarAnulacionCobro(IReadOnlyCollection<RegistrarPagoVenta> reintegros, string motivo, Guid usuarioId)
    {
        ValidarUsuario(usuarioId);
        if (Estado != EstadoVenta.Pagada || reintegros is null || reintegros.Count != _pagos.Count ||
            reintegros.Any(refund => refund.MedioPagoId == Guid.Empty || refund.Importe <= 0))
            throw new DomainException("La anulacion debe registrar un reintegro por cada pago original.");

        var reversals = new List<ReintegroVenta>(_pagos.Count);
        for (var index = 0; index < _pagos.Count; index++)
        {
            var refund = reintegros.ElementAt(index);
            if (refund.Importe != _pagos[index].Importe)
                throw new DomainException("El importe de la anulacion debe coincidir con cada pago original.");
            var original = _pagos[index];
            var reversal = new ReintegroVenta(Id, refund.MedioPagoId,
                $"Anulacion - {original.NombreMedioPago}", refund.Importe, refund.Referencia,
                refund.Observacion, usuarioId, original.AfectaEfectivo, true);
            reversals.Add(reversal);
        }
        _reintegros.AddRange(reversals);
        MotivoAnulacion = Normalizar(motivo, 500, "El motivo de anulacion");
        AnuladaEnUtc = DateTimeOffset.UtcNow;
        AnuladaPorUsuarioId = usuarioId;
        Estado = EstadoVenta.Anulada;
        ActualizadaPorUsuarioId = usuarioId;
        ActualizadaEnUtc = AnuladaEnUtc.Value;
    }

    public decimal ReemplazarPagosPorReintegro(IReadOnlyCollection<RegistrarPagoVenta> reintegros, string motivo, Guid usuarioId)
    {
        ValidarUsuario(usuarioId);
        if (Estado is not (EstadoVenta.Pagada or EstadoVenta.DevueltaParcialmente) || reintegros is null || reintegros.Count == 0 ||
            reintegros.Any(pago => pago.MedioPagoId == Guid.Empty || pago.Importe <= 0))
        {
            throw new DomainException("Los reintegros deben corresponder a una venta pagada y tener importes validos.");
        }

        var totalReintegrado = decimal.Round(reintegros.Sum(pago => decimal.Round(pago.Importe, 2)), 2);
        _ = Normalizar(motivo, 500, "El motivo de devolucion");
        var reintegradoAnterior = decimal.Round(_reintegros.Where(item => !item.EsAnulacion).Sum(item => item.Importe), 2);
        if (reintegradoAnterior + totalReintegrado > Total)
        {
            throw new DomainException("El reintegro no puede superar el total pagado.");
        }

        var pendingRefunds = reintegros.ToArray();
        if (pendingRefunds.Any(refund => _pagos.All(payment => payment.MedioPagoId != refund.MedioPagoId)))
            throw new DomainException("El reintegro debe utilizar un medio de pago de la venta original.");
        foreach (var group in pendingRefunds.GroupBy(refund => refund.MedioPagoId))
        {
            var previouslyRefunded = _reintegros.Where(item => !item.EsAnulacion && item.MedioPagoId == group.Key).Sum(item => item.Importe);
            var originalPaid = _pagos.Where(payment => payment.MedioPagoId == group.Key).Sum(payment => payment.Importe);
            if (previouslyRefunded + group.Sum(item => item.Importe) > originalPaid)
                throw new DomainException("El reintegro por medio de pago no puede superar el importe originalmente cobrado.");
        }
        foreach (var refund in pendingRefunds)
        {
            var original = _pagos.First(payment => payment.MedioPagoId == refund.MedioPagoId);
            var refundRecord = new ReintegroVenta(Id, refund.MedioPagoId,
                $"Reintegro - {original.NombreMedioPago}", refund.Importe,
                refund.Referencia, refund.Observacion,
                usuarioId, original.AfectaEfectivo, false);
            _reintegros.Add(refundRecord);
        }
        Estado = reintegradoAnterior + totalReintegrado == Total ? EstadoVenta.Devuelta : EstadoVenta.DevueltaParcialmente;
        MotivoDevolucion = motivo.Trim();
        DevueltaEnUtc = DateTimeOffset.UtcNow;
        DevueltaPorUsuarioId = usuarioId;
        ActualizadaPorUsuarioId = usuarioId;
        ActualizadaEnUtc = DateTimeOffset.UtcNow;
        return totalReintegrado;
    }

    public string? MotivoDevolucion { get; private set; }
    public DateTimeOffset? DevueltaEnUtc { get; private set; }
    public Guid? DevueltaPorUsuarioId { get; private set; }

    public void Anular(string motivo, Guid usuarioId)
    {
        ValidarUsuario(usuarioId);
        if (Estado is not (EstadoVenta.PendienteDeCobro or EstadoVenta.Pagada))
        {
            throw new DomainException("Solo se pueden anular ventas pendientes o pagadas.");
        }

        MotivoAnulacion = Normalizar(motivo, 500, "El motivo de anulacion");
        Estado = EstadoVenta.Anulada;
        AnuladaEnUtc = DateTimeOffset.UtcNow;
        AnuladaPorUsuarioId = usuarioId;
        ActualizarAuditoria(usuarioId);
    }

    private void ValidarDescuentoConDetalle(Guid productoId, decimal cantidad, decimal precio, decimal descuento)
    {
        var normalizedQuantity = decimal.Round(cantidad, 3);
        var normalizedPrice = decimal.Round(precio, 2);
        var normalizedDiscount = decimal.Round(descuento, 2);
        if (normalizedQuantity <= 0 || normalizedPrice < 0 || normalizedDiscount < 0 ||
            normalizedDiscount > decimal.Round(normalizedQuantity * normalizedPrice, 2))
        {
            throw new DomainException("La cantidad, el precio o el descuento del producto no son validos.");
        }

        var otherDetails = _detalles.Where(detalle => detalle.ProductoId != productoId).ToArray();
        var candidateGross = decimal.Round(
            otherDetails.Sum(detalle => detalle.SubtotalBruto) +
            decimal.Round(normalizedQuantity * normalizedPrice, 2) +
            ImporteManoObra,
            2);
        var candidateLineDiscount = decimal.Round(
            otherDetails.Sum(detalle => detalle.Descuento) + normalizedDiscount,
            2);
        if (DescuentoGeneral > candidateGross - candidateLineDiscount)
        {
            throw new DomainException("El descuento general no puede superar el nuevo importe de la venta.");
        }
    }

    private void ValidarPendiente(Guid usuarioId)
    {
        if (Estado != EstadoVenta.PendienteDeCobro)
        {
            throw new DomainException("Solo se pueden modificar ventas pendientes de cobro.");
        }

        ValidarUsuario(usuarioId);
    }

    private void ActualizarAuditoria(Guid usuarioId)
    {
        ActualizadaEnUtc = DateTimeOffset.UtcNow;
        ActualizadaPorUsuarioId = usuarioId;
    }

    private static void ValidarUsuario(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new DomainException("Debe registrarse el usuario responsable de la operacion.");
        }
    }

    private static string Normalizar(string? value, int maxLength, string field)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maxLength)
        {
            throw new DomainException($"{field} es obligatorio y no puede superar los {maxLength} caracteres.");
        }

        return normalized;
    }

    private static string? NormalizarOpcional(string? value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Normalizar(value, maxLength, field);
    }
}
