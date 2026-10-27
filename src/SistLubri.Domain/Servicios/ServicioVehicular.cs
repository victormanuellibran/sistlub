using SistLubri.Domain.Common;
using SistLubri.Domain.Productos;

namespace SistLubri.Domain.Servicios;

public sealed class ServicioVehicular : Entity
{
    private readonly List<DetalleServicio> _detalles = [];

    private ServicioVehicular()
    {
    }

    public ServicioVehicular(
        Guid vehiculoId,
        Guid clienteId,
        string patente,
        string clienteNombre,
        int kilometrajeIngreso,
        string trabajoSolicitado,
        string? inspeccionPrevia,
        string? danosExistentes,
        string? observaciones,
        decimal manoObra,
        Guid? usuarioId = null)
    {
        if (vehiculoId == Guid.Empty || clienteId == Guid.Empty)
        {
            throw new DomainException("El servicio debe estar asociado a un vehiculo y un cliente.");
        }

        if (kilometrajeIngreso < 0)
        {
            throw new DomainException("El kilometraje de ingreso no puede ser negativo.");
        }

        if (manoObra < 0)
        {
            throw new DomainException("El importe de mano de obra no puede ser negativo.");
        }

        VehiculoId = vehiculoId;
        ClienteId = clienteId;
        Patente = Normalizar(patente, 10, "La patente");
        ClienteNombre = Normalizar(clienteNombre, 201, "El cliente");
        TrabajoSolicitado = Normalizar(trabajoSolicitado, 500, "El trabajo solicitado");
        InspeccionPrevia = NormalizarOpcional(inspeccionPrevia, 2000, "La inspeccion previa");
        DanosExistentes = NormalizarOpcional(danosExistentes, 2000, "Los danos existentes");
        Observaciones = NormalizarOpcional(observaciones, 2000, "Las observaciones");
        KilometrajeIngreso = kilometrajeIngreso;
        ManoObra = decimal.Round(manoObra, 2);
        Estado = EstadoServicio.EnCurso;
        CreadoEnUtc = DateTimeOffset.UtcNow;
        CreadoPorUsuarioId = usuarioId;
    }

    public Guid VehiculoId { get; private init; }

    public Guid ClienteId { get; private init; }

    public string Patente { get; private init; } = string.Empty;

    public string ClienteNombre { get; private init; } = string.Empty;

    public int KilometrajeIngreso { get; private init; }

    public string TrabajoSolicitado { get; private set; } = string.Empty;

    public string? InspeccionPrevia { get; private init; }

    public string? DanosExistentes { get; private init; }

    public string? Observaciones { get; private set; }

    public decimal ManoObra { get; private set; }

    public EstadoServicio Estado { get; private set; }

    public DateTimeOffset CreadoEnUtc { get; private init; }

    public Guid? CreadoPorUsuarioId { get; private init; }

    public Guid? ActualizadoPorUsuarioId { get; private set; }

    public DateTimeOffset? ActualizadoEnUtc { get; private set; }

    public Guid? FinalizadoPorUsuarioId { get; private set; }

    public Guid? AnuladoPorUsuarioId { get; private set; }

    public DateTimeOffset? FinalizadoEnUtc { get; private set; }

    public DateTimeOffset? AnuladoEnUtc { get; private set; }

    public string? MotivoAnulacion { get; private set; }

    public IReadOnlyCollection<DetalleServicio> Detalles => _detalles.AsReadOnly();

    public decimal TotalProductos => decimal.Round(_detalles.Sum(detalle => detalle.Subtotal), 2);

    public decimal Total => decimal.Round(TotalProductos + ManoObra, 2);

    public void AgregarOActualizarDetalle(
        Guid productoId,
        string codigoProducto,
        string nombreProducto,
        UnidadMedidaProducto unidadMedida,
        bool esFraccionado,
        decimal cantidad,
        decimal precioUnitario,
        decimal costoUnitario,
        Guid? usuarioId = null)
    {
        if (Estado != EstadoServicio.EnCurso)
        {
            throw new DomainException("Solo se pueden agregar productos a un servicio en curso.");
        }

        var existing = _detalles.SingleOrDefault(detalle => detalle.ProductoId == productoId);
        if (existing is not null)
        {
            existing.ActualizarDatos(
                codigoProducto,
                nombreProducto,
                unidadMedida,
                esFraccionado,
                cantidad,
                precioUnitario,
                costoUnitario);
            RegistrarActualizacion(usuarioId);
            return;
        }

        _detalles.Add(new DetalleServicio(
            Id,
            productoId,
            codigoProducto,
            nombreProducto,
            unidadMedida,
            esFraccionado,
            cantidad,
            precioUnitario,
            costoUnitario));
        RegistrarActualizacion(usuarioId);
    }

    public void EliminarDetallesExcepto(IReadOnlySet<Guid> productoIds)
    {
        if (Estado != EstadoServicio.EnCurso)
        {
            throw new DomainException("Solo se pueden modificar los productos de un servicio en curso.");
        }

        _detalles.RemoveAll(detalle => !productoIds.Contains(detalle.ProductoId));
    }

    public void ActualizarDatosEnCurso(string trabajoSolicitado, string? observaciones, decimal manoObra, Guid? usuarioId = null)
    {
        if (Estado != EstadoServicio.EnCurso)
        {
            throw new DomainException("Solo se pueden modificar los datos de un servicio en curso.");
        }

        if (manoObra < 0)
        {
            throw new DomainException("El importe de mano de obra no puede ser negativo.");
        }

        TrabajoSolicitado = Normalizar(trabajoSolicitado, 500, "El trabajo solicitado");
        Observaciones = NormalizarOpcional(observaciones, 2000, "Las observaciones");
        ManoObra = decimal.Round(manoObra, 2);
        RegistrarActualizacion(usuarioId);
    }

    public void Finalizar(Guid? usuarioId = null)
    {
        if (Estado != EstadoServicio.EnCurso)
        {
            throw new DomainException("Solo se puede finalizar un servicio en curso.");
        }

        FinalizadoEnUtc = DateTimeOffset.UtcNow;
        FinalizadoPorUsuarioId = usuarioId;
        Estado = EstadoServicio.Finalizado;
        RegistrarActualizacion(usuarioId);
    }

    public void ConfirmarConsumo(IReadOnlyDictionary<Guid, decimal> cantidadesUtilizadas, Guid? usuarioId = null)
    {
        if (Estado != EstadoServicio.EnCurso)
        {
            throw new DomainException("Solo se puede confirmar el consumo de un servicio en curso.");
        }

        ArgumentNullException.ThrowIfNull(cantidadesUtilizadas);
        if (cantidadesUtilizadas.Keys.Any(productId => _detalles.All(detail => detail.ProductoId != productId)))
        {
            throw new DomainException("El consumo incluye un producto que no estaba reservado para el servicio.");
        }

        foreach (var detail in _detalles)
        {
            var quantity = cantidadesUtilizadas.GetValueOrDefault(detail.ProductoId);
            detail.ConfirmarCantidadUtilizada(quantity);
        }

        _detalles.RemoveAll(detail => detail.Cantidad == 0);
        RegistrarActualizacion(usuarioId);
    }

    public void Anular(string motivo, Guid? usuarioId = null)
    {
        if (Estado != EstadoServicio.EnCurso)
        {
            throw new DomainException("En esta etapa solo se pueden anular servicios que aun no fueron finalizados.");
        }

        MotivoAnulacion = Normalizar(motivo, 500, "El motivo de anulacion");
        AnuladoEnUtc = DateTimeOffset.UtcNow;
        AnuladoPorUsuarioId = usuarioId;
        Estado = EstadoServicio.Anulado;
        RegistrarActualizacion(usuarioId);
    }

    private void RegistrarActualizacion(Guid? usuarioId)
    {
        ActualizadoPorUsuarioId = usuarioId;
        ActualizadoEnUtc = DateTimeOffset.UtcNow;
    }

    private static string Normalizar(string? value, int maxLength, string field)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new DomainException($"{field} es obligatorio.");
        }

        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{field} no puede superar los {maxLength} caracteres.");
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
