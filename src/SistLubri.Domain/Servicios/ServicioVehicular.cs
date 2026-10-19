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
        decimal manoObra)
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
    }

    public Guid VehiculoId { get; private init; }

    public Guid ClienteId { get; private init; }

    public string Patente { get; private init; } = string.Empty;

    public string ClienteNombre { get; private init; } = string.Empty;

    public int KilometrajeIngreso { get; private init; }

    public string TrabajoSolicitado { get; private init; } = string.Empty;

    public string? InspeccionPrevia { get; private init; }

    public string? DanosExistentes { get; private init; }

    public string? Observaciones { get; private init; }

    public decimal ManoObra { get; private init; }

    public EstadoServicio Estado { get; private set; }

    public DateTimeOffset CreadoEnUtc { get; private init; }

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
        decimal costoUnitario)
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
    }

    public void EliminarDetallesExcepto(IReadOnlySet<Guid> productoIds)
    {
        if (Estado != EstadoServicio.EnCurso)
        {
            throw new DomainException("Solo se pueden modificar los productos de un servicio en curso.");
        }

        _detalles.RemoveAll(detalle => !productoIds.Contains(detalle.ProductoId));
    }

    public void Finalizar()
    {
        if (Estado != EstadoServicio.EnCurso)
        {
            throw new DomainException("Solo se puede finalizar un servicio en curso.");
        }

        FinalizadoEnUtc = DateTimeOffset.UtcNow;
        Estado = EstadoServicio.Finalizado;
    }

    public void Anular(string motivo)
    {
        if (Estado != EstadoServicio.EnCurso)
        {
            throw new DomainException("En esta etapa solo se pueden anular servicios que aun no fueron finalizados.");
        }

        MotivoAnulacion = Normalizar(motivo, 500, "El motivo de anulacion");
        AnuladoEnUtc = DateTimeOffset.UtcNow;
        Estado = EstadoServicio.Anulado;
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
