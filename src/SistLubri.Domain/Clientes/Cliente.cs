using System.Net.Mail;
using SistLubri.Domain.Common;

namespace SistLubri.Domain.Clientes;

public sealed class Cliente : Entity
{
    private Cliente()
    {
    }

    public Cliente(
        string nombre,
        string apellido,
        string telefono,
        string? email = null,
        string? domicilio = null,
        string? observaciones = null)
    {
        ActualizarDatos(nombre, apellido, telefono, email, domicilio, observaciones);
        Activo = true;
        CreadoEnUtc = DateTimeOffset.UtcNow;
    }

    public string Nombre { get; private set; } = string.Empty;

    public string Apellido { get; private set; } = string.Empty;

    public string Telefono { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public string? Domicilio { get; private set; }

    public string? Observaciones { get; private set; }

    public bool Activo { get; private set; }

    public DateTimeOffset CreadoEnUtc { get; private init; }

    public DateTimeOffset? ActualizadoEnUtc { get; private set; }

    public DateTimeOffset? DesactivadoEnUtc { get; private set; }

    public void ActualizarDatos(
        string nombre,
        string apellido,
        string telefono,
        string? email,
        string? domicilio,
        string? observaciones)
    {
        Nombre = NormalizarRequerido(nombre, 100, "El nombre");
        Apellido = NormalizarRequerido(apellido, 100, "El apellido");
        Telefono = NormalizarTelefono(telefono);
        Email = NormalizarEmail(email);
        Domicilio = NormalizarOpcional(domicilio, 250, "El domicilio");
        Observaciones = NormalizarOpcional(observaciones, 1000, "Las observaciones");
        ActualizadoEnUtc = DateTimeOffset.UtcNow;
    }

    public void Desactivar()
    {
        if (!Activo)
        {
            throw new DomainException("El cliente ya se encuentra inactivo.");
        }

        Activo = false;
        DesactivadoEnUtc = DateTimeOffset.UtcNow;
        ActualizadoEnUtc = DesactivadoEnUtc;
    }

    public void Reactivar()
    {
        if (Activo)
        {
            throw new DomainException("El cliente ya se encuentra activo.");
        }

        Activo = true;
        DesactivadoEnUtc = null;
        ActualizadoEnUtc = DateTimeOffset.UtcNow;
    }

    private static string NormalizarRequerido(string? valor, int longitudMaxima, string campo)
    {
        var normalizado = valor?.Trim();
        if (string.IsNullOrWhiteSpace(normalizado))
        {
            throw new DomainException($"{campo} es obligatorio.");
        }

        if (normalizado.Length > longitudMaxima)
        {
            throw new DomainException($"{campo} no puede superar los {longitudMaxima} caracteres.");
        }

        return normalizado;
    }

    private static string NormalizarTelefono(string? telefono)
    {
        var valor = NormalizarRequerido(telefono, 30, "El telefono");

        if (valor.Any(c => !char.IsDigit(c) && !char.IsWhiteSpace(c) && c is not '+' and not '-' and not '(' and not ')'))
        {
            throw new DomainException("El telefono contiene caracteres no permitidos.");
        }

        var digitos = new string(valor.Where(char.IsDigit).ToArray());
        if (digitos.Length is < 7 or > 15)
        {
            throw new DomainException("El telefono debe contener entre 7 y 15 digitos.");
        }

        return digitos;
    }

    private static string? NormalizarEmail(string? email)
    {
        var normalizado = NormalizarOpcional(email, 254, "El correo electronico");
        if (normalizado is null)
        {
            return null;
        }

        if (!MailAddress.TryCreate(normalizado, out _))
        {
            throw new DomainException("El correo electronico no tiene un formato valido.");
        }

        return normalizado.ToLowerInvariant();
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
