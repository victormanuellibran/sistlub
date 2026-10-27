using SistLubri.Domain.Common;

namespace SistLubri.Domain.Proveedores;

public sealed class RegistroEstadoProveedor : Entity
{
    private RegistroEstadoProveedor() { }

    internal RegistroEstadoProveedor(Guid proveedorId, EstadoProveedor estado, string? motivo, Guid usuarioId)
    {
        if (proveedorId == Guid.Empty || usuarioId == Guid.Empty || !Enum.IsDefined(estado))
            throw new DomainException("Los datos del cambio de estado del proveedor no son validos.");

        ProveedorId = proveedorId;
        Estado = estado;
        Motivo = motivo;
        FechaUtc = DateTimeOffset.UtcNow;
        UsuarioId = usuarioId;
    }

    public Guid ProveedorId { get; private set; }
    public Proveedor Proveedor { get; private init; } = null!;
    public EstadoProveedor Estado { get; private init; }
    public string? Motivo { get; private init; }
    public DateTimeOffset FechaUtc { get; private init; }
    public Guid UsuarioId { get; private init; }
}
