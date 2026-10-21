using SistLubri.Domain.Clientes;
using SistLubri.Domain.Common;

namespace SistLubri.Domain.Vehiculos;

public sealed class Vehiculo : Entity
{
    private Vehiculo()
    {
    }

    public Vehiculo(
        Guid clienteId,
        string patente,
        string marca,
        string modelo,
        int? anio = null,
        int? kilometrajeActual = null,
        int? proximoCambioKilometraje = null,
        string? observaciones = null)
    {
        CambiarTitular(clienteId);
        Patente = NormalizarPatente(patente);
        ActualizarDatos(marca, modelo, anio, kilometrajeActual, proximoCambioKilometraje, observaciones);
        CreadoEnUtc = DateTimeOffset.UtcNow;
    }

    public Guid ClienteId { get; private set; }

    public Cliente Cliente { get; private set; } = null!;

    public string Patente { get; private set; } = string.Empty;

    public string Marca { get; private set; } = string.Empty;

    public string Modelo { get; private set; } = string.Empty;

    public int? Anio { get; private set; }

    public int? KilometrajeActual { get; private set; }

    public int? ProximoCambioKilometraje { get; private set; }

    public string? Observaciones { get; private set; }

    public DateTimeOffset CreadoEnUtc { get; private init; }

    public DateTimeOffset? ActualizadoEnUtc { get; private set; }

    public void ActualizarDatos(
        string marca,
        string modelo,
        int? anio,
        int? kilometrajeActual,
        int? proximoCambioKilometraje,
        string? observaciones)
    {
        Marca = NormalizarRequerido(marca, 80, "La marca");
        Modelo = NormalizarRequerido(modelo, 80, "El modelo");

        if (anio is < 1900 or > 2100)
        {
            throw new DomainException("El anio del vehiculo debe estar entre 1900 y 2100.");
        }

        if (kilometrajeActual < 0)
        {
            throw new DomainException("El kilometraje no puede ser negativo.");
        }

        if (KilometrajeActual.HasValue
            && kilometrajeActual.HasValue
            && kilometrajeActual < KilometrajeActual)
        {
            throw new DomainException(
                "El kilometraje actual no puede ser menor al ultimo kilometraje registrado.");
        }

        if (proximoCambioKilometraje < 0)
        {
            throw new DomainException("El kilometraje del proximo cambio no puede ser negativo.");
        }

        if (kilometrajeActual.HasValue
            && proximoCambioKilometraje.HasValue
            && proximoCambioKilometraje <= kilometrajeActual)
        {
            throw new DomainException(
                "El kilometraje del proximo cambio debe ser mayor al kilometraje actual.");
        }

        Anio = anio;
        KilometrajeActual = kilometrajeActual;
        ProximoCambioKilometraje = proximoCambioKilometraje;
        Observaciones = NormalizarOpcional(observaciones, 1000, "Las observaciones");
        ActualizadoEnUtc = DateTimeOffset.UtcNow;
    }

    public void CambiarTitular(Guid clienteId)
    {
        if (clienteId == Guid.Empty)
        {
            throw new DomainException("El vehiculo debe estar asociado a un cliente.");
        }

        ClienteId = clienteId;
        ActualizadoEnUtc = DateTimeOffset.UtcNow;
    }

    public static string NormalizarPatente(string? patente)
    {
        var valor = NormalizarRequerido(patente, 12, "La patente");
        if (valor.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c) && c != '-'))
        {
            throw new DomainException("La patente contiene caracteres no permitidos.");
        }

        var normalizada = new string(valor.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (normalizada.Length is < 5 or > 10)
        {
            throw new DomainException("La patente debe contener entre 5 y 10 letras o numeros.");
        }

        return normalizada;
    }

    private static string NormalizarRequerido(string? valor, int longitudMaxima, string campo)
    {
        var normalizado = valor?.Trim();
        if (string.IsNullOrWhiteSpace(normalizado))
        {
            throw new DomainException($"{campo} es obligatoria.");
        }

        if (normalizado.Length > longitudMaxima)
        {
            throw new DomainException($"{campo} no puede superar los {longitudMaxima} caracteres.");
        }

        return normalizado;
    }

    private static string? NormalizarOpcional(string? valor, int longitudMaxima, string campo)
    {
        var normalizado = valor?.Trim();
        if (string.IsNullOrWhiteSpace(normalizado))
        {
            return null;
        }

        if (normalizado.Length > longitudMaxima)
        {
            throw new DomainException($"{campo} no puede superar los {longitudMaxima} caracteres.");
        }

        return normalizado;
    }
}
