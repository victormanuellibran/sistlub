using SistLubri.Domain.Common;
using SistLubri.Domain.Productos;
using SistLubri.Domain.Proveedores;

namespace SistLubri.Domain.Compras;

public sealed class Compra : Entity
{
    private readonly List<DetalleCompra> _detalles = [];
    private readonly List<RecepcionCompra> _recepciones = [];
    private readonly List<FacturaCompra> _facturas = [];

    private Compra() { }

    public Compra(Proveedor proveedor, DateOnly fecha, string? observaciones,
        IReadOnlyCollection<DetalleCompraInicial> detalles, Guid usuarioId)
    {
        ArgumentNullException.ThrowIfNull(proveedor);
        ValidarProveedorYUsuario(proveedor.Id, usuarioId);
        if (!proveedor.Activo) throw new DomainException("No se puede crear una compra con un proveedor inactivo.");
        if (fecha == default) throw new DomainException("La fecha de la orden no es valida.");
        if (detalles is null || detalles.Count == 0 || detalles.Count > 100)
            throw new DomainException("La orden debe contener entre 1 y 100 productos.");
        if (detalles.Select(item => item.ProductoId).Distinct().Count() != detalles.Count)
            throw new DomainException("No se puede repetir un producto en la misma orden.");

        ProveedorId = proveedor.Id;
        Proveedor = proveedor;
        Fecha = fecha;
        Observaciones = NormalizarOpcional(observaciones, 1000);
        CreadoPorUsuarioId = usuarioId;
        CreadoEnUtc = DateTimeOffset.UtcNow;
        ActualizadoPorUsuarioId = usuarioId;
        ActualizadoEnUtc = CreadoEnUtc;
        foreach (var item in detalles)
            _detalles.Add(new DetalleCompra(item.ProductoId, item.CodigoProducto, item.NombreProducto,
                item.UnidadMedida, item.CantidadSolicitada, item.CostoUnitarioEstimado));
    }

    public Guid ProveedorId { get; private set; }
    public Proveedor Proveedor { get; private set; } = null!;
    public DateOnly Fecha { get; private set; }
    public string? Observaciones { get; private set; }
    public DateTimeOffset CreadoEnUtc { get; private init; }
    public Guid CreadoPorUsuarioId { get; private init; }
    public DateTimeOffset ActualizadoEnUtc { get; private set; }
    public Guid ActualizadoPorUsuarioId { get; private set; }
    public DateTimeOffset? CerradaEnUtc { get; private set; }
    public Guid? CerradaPorUsuarioId { get; private set; }
    public string? MotivoCierre { get; private set; }
    public DateTimeOffset? AnuladaEnUtc { get; private set; }
    public Guid? AnuladaPorUsuarioId { get; private set; }
    public string? MotivoAnulacion { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<DetalleCompra> Detalles => _detalles.AsReadOnly();
    public IReadOnlyCollection<RecepcionCompra> Recepciones => _recepciones.AsReadOnly();
    public IReadOnlyCollection<FacturaCompra> Facturas => _facturas.AsReadOnly();
    public decimal TotalEstimado => decimal.Round(_detalles.Sum(item => item.CantidadSolicitada * item.CostoUnitarioEstimado), 2);
    public decimal TotalFacturado => decimal.Round(_facturas.Sum(item => item.ImporteTotal), 2);
    public decimal TotalPagado => decimal.Round(_facturas.Sum(item => item.TotalPagado), 2);
    public decimal SaldoPendiente => decimal.Round(_facturas.Sum(item => item.SaldoPendiente), 2);
    public EstadoRecepcionCompra EstadoRecepcion => AnuladaEnUtc.HasValue ? EstadoRecepcionCompra.Anulada
        : CerradaEnUtc.HasValue ? EstadoRecepcionCompra.CerradaConDiferencias
        : _detalles.All(item => item.CantidadPendiente == 0) ? EstadoRecepcionCompra.Completa
        : _detalles.Any(item => item.CantidadAceptada > 0) ? EstadoRecepcionCompra.Parcial
        : EstadoRecepcionCompra.Pendiente;
    public EstadoPagoCompra EstadoPago => _facturas.Count == 0 ? EstadoPagoCompra.SinFactura
        : SaldoPendiente == 0 ? EstadoPagoCompra.Pagada
        : TotalPagado > 0 ? EstadoPagoCompra.Parcial
        : EstadoPagoCompra.Pendiente;

    public RecepcionCompra RegistrarRecepcion(DateOnly fecha, string? remito, string? observaciones,
        IReadOnlyCollection<LineaRecepcionInicial> lineas, IReadOnlyDictionary<Guid, Producto> productos, Guid usuarioId)
    {
        ValidarRecepcionAbierta(usuarioId);
        if (fecha == default || fecha < Fecha) throw new DomainException("La fecha de recepcion no puede ser anterior a la orden.");
        if (lineas is null || lineas.Count == 0 || lineas.Select(item => item.DetalleCompraId).Distinct().Count() != lineas.Count)
            throw new DomainException("La recepcion debe incluir lineas validas y sin duplicados.");

        var receipt = new RecepcionCompra(Id, fecha, remito, observaciones, usuarioId);
        foreach (var line in lineas)
        {
            var detail = _detalles.SingleOrDefault(item => item.Id == line.DetalleCompraId)
                ?? throw new DomainException("La linea de recepcion no pertenece a esta orden.");
            if (!productos.TryGetValue(detail.ProductoId, out var product) || !product.Activo)
                throw new DomainException($"El producto '{detail.NombreProducto}' no existe o esta inactivo.");
            if (line.CantidadAceptada < 0 || line.CantidadDanada < 0 || line.CantidadAceptada + line.CantidadDanada <= 0)
                throw new DomainException("La recepcion debe registrar cantidades aceptadas o danadas mayores que cero.");
            if (line.CantidadAceptada > detail.CantidadPendiente)
                throw new DomainException($"La cantidad aceptada supera lo pendiente de '{detail.NombreProducto}'.");
            if (line.CostoUnitario < 0) throw new DomainException("El costo unitario recibido no puede ser negativo.");
            if (line.CantidadAceptada > 0)
            {
                product.RegistrarCostoCompra(line.CantidadAceptada, line.CostoUnitario);
                product.RegistrarMovimiento(TipoMovimientoInventario.EntradaCompra, line.CantidadAceptada,
                    $"Recepcion de compra {Id}", receipt.Id, usuarioId);
                detail.Aceptar(line.CantidadAceptada);
            }
            if (line.CantidadDanada > 0) detail.RegistrarDanada(line.CantidadDanada);
            receipt.AgregarLinea(new LineaRecepcionCompra(detail.Id, line.CantidadAceptada,
                line.CantidadDanada, line.CostoUnitario, line.Observaciones));
        }
        _recepciones.Add(receipt);
        Actualizar(usuarioId);
        return receipt;
    }

    public void ActualizarDatos(Proveedor proveedor, DateOnly fecha, string? observaciones,
        IReadOnlyCollection<DetalleCompraInicial> detalles, Guid usuarioId)
    {
        ArgumentNullException.ThrowIfNull(proveedor);
        ValidarRecepcionAbierta(usuarioId);
        if (_recepciones.Count > 0 || _facturas.Count > 0)
            throw new DomainException("Una orden con recepciones o facturas no se puede modificar; registre una correccion.");
        if (proveedor.Id == Guid.Empty || !proveedor.Activo || fecha == default)
            throw new DomainException("El proveedor o la fecha de la orden no son validos.");
        if (detalles is null || detalles.Count == 0 || detalles.Count > 100 || detalles.Select(item => item.ProductoId).Distinct().Count() != detalles.Count)
            throw new DomainException("La orden debe tener entre 1 y 100 productos diferentes.");
        ProveedorId = proveedor.Id;
        Proveedor = proveedor;
        Fecha = fecha;
        Observaciones = NormalizarOpcional(observaciones, 1000);
        var pending = detalles.ToDictionary(item => item.ProductoId);
        foreach (var current in _detalles.ToArray())
        {
            if (!pending.Remove(current.ProductoId, out var replacement))
            {
                _detalles.Remove(current);
                continue;
            }

            current.ActualizarDatos(replacement);
        }

        foreach (var item in pending.Values)
            _detalles.Add(new DetalleCompra(item.ProductoId, item.CodigoProducto, item.NombreProducto,
                item.UnidadMedida, item.CantidadSolicitada, item.CostoUnitarioEstimado));
        Actualizar(usuarioId);
    }

    public void RegistrarFactura(string numero, DateOnly fecha, DateOnly? vencimiento, decimal importe,
        string? observaciones, Guid usuarioId)
    {
        ValidarActiva(usuarioId);
        if (string.IsNullOrWhiteSpace(numero) || numero.Trim().Length > 120)
            throw new DomainException("El numero de factura es obligatorio y no puede superar 120 caracteres.");
        if (fecha == default || fecha < Fecha || (vencimiento.HasValue && vencimiento.Value < fecha))
            throw new DomainException("Las fechas de la factura no son validas.");
        if (importe <= 0) throw new DomainException("El importe de la factura debe ser mayor que cero.");
        _facturas.Add(new FacturaCompra(Id, numero, fecha, vencimiento, importe, observaciones, usuarioId));
        Actualizar(usuarioId);
    }

    public PagoCompra RegistrarPago(Guid facturaId, DateOnly fecha, Guid medioPagoId, decimal importe,
        string? referencia, string? observacion, Guid usuarioId)
    {
        ValidarActiva(usuarioId);
        var factura = _facturas.SingleOrDefault(item => item.Id == facturaId)
            ?? throw new DomainException("La factura no pertenece a esta compra.");
        var payment = factura.RegistrarPago(fecha, medioPagoId, importe, referencia, observacion, usuarioId);
        Actualizar(usuarioId);
        return payment;
    }

    public void CerrarRecepcion(string motivo, Guid usuarioId)
    {
        ValidarRecepcionAbierta(usuarioId);
        if (!_detalles.Any(item => item.CantidadPendiente > 0))
            throw new DomainException("La recepcion ya esta completa.");
        MotivoCierre = NormalizarRequerido(motivo, 500, "El motivo de cierre");
        CerradaEnUtc = DateTimeOffset.UtcNow;
        CerradaPorUsuarioId = usuarioId;
        Actualizar(usuarioId);
    }

    public void Anular(string motivo, Guid usuarioId)
    {
        ValidarActiva(usuarioId);
        if (_recepciones.Count > 0 || _facturas.Count > 0)
            throw new DomainException("Una orden con recepciones o facturas no se puede anular. Debe resolverse mediante devoluciones y notas de credito.");
        MotivoAnulacion = NormalizarRequerido(motivo, 500, "El motivo de anulacion");
        AnuladaEnUtc = DateTimeOffset.UtcNow;
        AnuladaPorUsuarioId = usuarioId;
        Actualizar(usuarioId);
    }

    public void MarcarActualizada(Guid usuarioId) => Actualizar(usuarioId);

    private void ValidarActiva(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty) throw new DomainException("El usuario responsable no es valido.");
        if (AnuladaEnUtc.HasValue) throw new DomainException("La orden esta anulada.");
    }

    private void ValidarRecepcionAbierta(Guid usuarioId)
    {
        ValidarActiva(usuarioId);
        if (CerradaEnUtc.HasValue) throw new DomainException("La recepcion de la orden fue cerrada.");
    }

    private void Actualizar(Guid usuarioId)
    {
        ActualizadoEnUtc = DateTimeOffset.UtcNow;
        ActualizadoPorUsuarioId = usuarioId;
    }

    private static void ValidarProveedorYUsuario(Guid proveedorId, Guid usuarioId)
    {
        if (proveedorId == Guid.Empty || usuarioId == Guid.Empty)
            throw new DomainException("El proveedor y el usuario responsable son obligatorios.");
    }

    private static string NormalizarRequerido(string? value, int max, string field)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > max)
            throw new DomainException($"{field} es obligatorio y no puede superar {max} caracteres.");
        return normalized;
    }

    private static string? NormalizarOpcional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > max) throw new DomainException($"El texto no puede superar {max} caracteres.");
        return normalized;
    }
}

public sealed record DetalleCompraInicial(Guid ProductoId, string CodigoProducto, string NombreProducto,
    string UnidadMedida, decimal CantidadSolicitada, decimal CostoUnitarioEstimado);

public sealed record LineaRecepcionInicial(Guid DetalleCompraId, decimal CantidadAceptada,
    decimal CantidadDanada, decimal CostoUnitario, string? Observaciones);
