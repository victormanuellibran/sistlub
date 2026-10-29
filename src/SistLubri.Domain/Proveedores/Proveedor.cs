using System.Net.Mail;
using SistLubri.Domain.Common;

namespace SistLubri.Domain.Proveedores;

public sealed class Proveedor : Entity
{
    private readonly List<RegistroEstadoProveedor> _historialEstados = [];

    private Proveedor() { }

    public Proveedor(
        string razonSocial,
        string? cuit,
        string? contactoNombre,
        string telefono,
        string? email,
        string? direccion,
        string? marcasYProductos,
        string? condicionPagoHabitual,
        int? plazoPagoHabitualDias,
        string? periodoPagoHabitual,
        string? observaciones,
        Guid creadoPorUsuarioId)
    {
        if (creadoPorUsuarioId == Guid.Empty)
            throw new DomainException("El usuario responsable del alta no es valido.");

        Activo = true;
        ActualizarDatos(razonSocial, cuit, contactoNombre, telefono, email, direccion,
            marcasYProductos, condicionPagoHabitual, plazoPagoHabitualDias, periodoPagoHabitual, observaciones);
        CreadoEnUtc = DateTimeOffset.UtcNow;
        CreadoPorUsuarioId = creadoPorUsuarioId;
    }

    public string RazonSocial { get; private set; } = string.Empty;
    public string? Cuit { get; private set; }
    public string? ContactoNombre { get; private set; }
    public string Telefono { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Direccion { get; private set; }
    public string? MarcasYProductos { get; private set; }
    public string? CondicionPagoHabitual { get; private set; }
    public int? PlazoPagoHabitualDias { get; private set; }
    public string? PeriodoPagoHabitual { get; private set; }
    public string? Observaciones { get; private set; }
    public bool Activo { get; private set; }
    public DateTimeOffset CreadoEnUtc { get; private init; }
    public Guid CreadoPorUsuarioId { get; private init; }
    public DateTimeOffset? ActualizadoEnUtc { get; private set; }
    public Guid? ActualizadoPorUsuarioId { get; private set; }
    public IReadOnlyCollection<RegistroEstadoProveedor> HistorialEstados => _historialEstados.AsReadOnly();
    public IReadOnlyCollection<RegistroEstadoProveedor> HistorialEstadosPendientesPersistencia =>
        _historialEstados.Where(entry => entry.Id != Guid.Empty).ToArray();

    public void ActualizarDatos(
        string razonSocial,
        string? cuit,
        string? contactoNombre,
        string telefono,
        string? email,
        string? direccion,
        string? marcasYProductos,
        string? condicionPagoHabitual,
        int? plazoPagoHabitualDias,
        string? periodoPagoHabitual,
        string? observaciones,
        Guid? usuarioId = null)
    {
        var nombreNormalizado = NormalizarRequerido(razonSocial, 160, "La razon social");
        var cuitNormalizado = NormalizarCuit(cuit);
        var contactoNormalizado = NormalizarOpcional(contactoNombre, 120, "El contacto");
        var telefonoNormalizado = NormalizarTelefono(telefono);
        var emailNormalizado = NormalizarEmail(email);
        var direccionNormalizada = NormalizarOpcional(direccion, 250, "La direccion");
        var marcasNormalizadas = NormalizarOpcional(marcasYProductos, 500, "Las marcas o productos");
        var condicionNormalizada = NormalizarOpcional(condicionPagoHabitual, 120, "La condicion de pago habitual");
        var periodoNormalizado = NormalizarOpcional(periodoPagoHabitual, 80, "El periodo de pago habitual");
        var observacionesNormalizadas = NormalizarOpcional(observaciones, 1000, "Las observaciones");

        if (plazoPagoHabitualDias is < 0 or > 3650)
            throw new DomainException("El plazo habitual de pago debe estar entre 0 y 3650 dias.");
        if (usuarioId == Guid.Empty)
            throw new DomainException("El usuario responsable de la modificacion no es valido.");

        if (!Activo && (nombreNormalizado != RazonSocial || cuitNormalizado != Cuit ||
            marcasNormalizadas != MarcasYProductos || condicionNormalizada != CondicionPagoHabitual ||
            plazoPagoHabitualDias != PlazoPagoHabitualDias || periodoNormalizado != PeriodoPagoHabitual))
        {
            throw new DomainException("Un proveedor inactivo solo permite actualizar contacto, direccion y observaciones.");
        }

        RazonSocial = nombreNormalizado;
        Cuit = cuitNormalizado;
        ContactoNombre = contactoNormalizado;
        Telefono = telefonoNormalizado;
        Email = emailNormalizado;
        Direccion = direccionNormalizada;
        MarcasYProductos = marcasNormalizadas;
        CondicionPagoHabitual = condicionNormalizada;
        PlazoPagoHabitualDias = plazoPagoHabitualDias;
        PeriodoPagoHabitual = periodoNormalizado;
        Observaciones = observacionesNormalizadas;
        if (usuarioId.HasValue)
        {
            ActualizadoPorUsuarioId = usuarioId.Value;
        }
        ActualizadoEnUtc = DateTimeOffset.UtcNow;
    }

    public void Desactivar(string motivo, Guid usuarioId)
    {
        if (!Activo)
            throw new DomainException("El proveedor ya se encuentra inactivo.");
        if (usuarioId == Guid.Empty)
            throw new DomainException("El usuario responsable de la baja no es valido.");

        var normalizedReason = NormalizarRequerido(motivo, 500, "El motivo de desactivacion");
        Activo = false;
        ActualizarEnAuditoria(usuarioId);
        _historialEstados.Add(new RegistroEstadoProveedor(Id, EstadoProveedor.Inactivo, normalizedReason, usuarioId));
    }

    public void Reactivar(Guid usuarioId)
    {
        if (Activo)
            throw new DomainException("El proveedor ya se encuentra activo.");
        if (usuarioId == Guid.Empty)
            throw new DomainException("El usuario responsable de la reactivacion no es valido.");

        Activo = true;
        ActualizarEnAuditoria(usuarioId);
        _historialEstados.Add(new RegistroEstadoProveedor(Id, EstadoProveedor.Activo, null, usuarioId));
    }

    public static string? NormalizarCuit(string? cuit)
    {
        var normalized = cuit?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return null;
        if (normalized.Any(character => !char.IsDigit(character) && !char.IsWhiteSpace(character) && character != '-'))
            throw new DomainException("El CUIT solo puede contener digitos, espacios y guiones.");

        var digits = new string(normalized.Where(char.IsDigit).ToArray());
        if (digits.Length != 11)
            throw new DomainException("El CUIT debe contener 11 digitos.");
        return digits;
    }

    private void ActualizarEnAuditoria(Guid usuarioId)
    {
        ActualizadoEnUtc = DateTimeOffset.UtcNow;
        ActualizadoPorUsuarioId = usuarioId;
    }

    private static string NormalizarTelefono(string? telefono)
    {
        var normalized = NormalizarRequerido(telefono, 30, "El telefono");
        if (normalized.Any(character => !char.IsDigit(character) && !char.IsWhiteSpace(character) &&
            character is not '+' and not '-' and not '(' and not ')'))
            throw new DomainException("El telefono contiene caracteres no permitidos.");

        var digits = new string(normalized.Where(char.IsDigit).ToArray());
        if (digits.Length is < 7 or > 15)
            throw new DomainException("El telefono debe contener entre 7 y 15 digitos.");
        return digits;
    }

    private static string? NormalizarEmail(string? email)
    {
        var normalized = NormalizarOpcional(email, 254, "El correo electronico");
        if (normalized is null) return null;
        if (!MailAddress.TryCreate(normalized, out _))
            throw new DomainException("El correo electronico no tiene un formato valido.");
        return normalized.ToLowerInvariant();
    }

    private static string NormalizarRequerido(string? value, int maxLength, string field)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new DomainException($"{field} es obligatorio.");
        if (normalized.Length > maxLength)
            throw new DomainException($"{field} no puede superar los {maxLength} caracteres.");
        return normalized;
    }

    private static string? NormalizarOpcional(string? value, int maxLength, string field)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return null;
        if (normalized.Length > maxLength)
            throw new DomainException($"{field} no puede superar los {maxLength} caracteres.");
        return normalized;
    }
}
