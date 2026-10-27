using SistLubri.Domain.Common;

namespace SistLubri.Domain.Caja;

public sealed class MedioPago : Entity
{
    private MedioPago() { }

    public MedioPago(string nombre, bool requiereReferencia = false, bool afectaEfectivo = false)
    {
        Nombre = Normalizar(nombre);
        RequiereReferencia = requiereReferencia;
        AfectaEfectivo = afectaEfectivo;
        Activo = true;
    }

    public string Nombre { get; private set; } = string.Empty;
    public bool RequiereReferencia { get; private set; }
    public bool AfectaEfectivo { get; private set; }
    public bool Activo { get; private set; }

    public void ValidarCobro(string? referencia)
    {
        if (!Activo)
        {
            throw new DomainException($"El medio de pago '{Nombre}' esta inactivo.");
        }

        if (RequiereReferencia && string.IsNullOrWhiteSpace(referencia))
        {
            throw new DomainException($"El medio de pago '{Nombre}' requiere una referencia.");
        }
    }

    public void Actualizar(string nombre, bool requiereReferencia, bool afectaEfectivo, bool activo)
    {
        Nombre = Normalizar(nombre);
        RequiereReferencia = requiereReferencia;
        AfectaEfectivo = afectaEfectivo;
        Activo = activo;
    }

    private static string Normalizar(string? nombre)
    {
        var value = nombre?.Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 60)
        {
            throw new DomainException("El nombre del medio de pago es obligatorio y no puede superar 60 caracteres.");
        }
        return value;
    }
}
