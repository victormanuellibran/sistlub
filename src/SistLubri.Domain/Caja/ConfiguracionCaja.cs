using SistLubri.Domain.Common;

namespace SistLubri.Domain.Caja;

public sealed class ConfiguracionCaja : Entity
{
    private ConfiguracionCaja() { }

    public ConfiguracionCaja(TimeOnly horaApertura, TimeOnly horaCierre, TimeSpan? duracionJornada, bool aperturaAutomatica, bool cierreAutomatico)
    {
        if ((horaApertura == horaCierre && duracionJornada is null && !cierreAutomatico) ||
            (duracionJornada.HasValue && (duracionJornada.Value <= TimeSpan.Zero || duracionJornada.Value > TimeSpan.FromHours(24))))
        {
            throw new DomainException("La hora de apertura y cierre no pueden ser iguales.");
        }
        HoraApertura = horaApertura;
        HoraCierre = horaCierre;
        DuracionJornada = duracionJornada;
        AperturaAutomatica = aperturaAutomatica;
        CierreAutomatico = cierreAutomatico;
        ActualizadaEnUtc = DateTimeOffset.UtcNow;
    }

    public TimeOnly HoraApertura { get; private set; }
    public TimeOnly HoraCierre { get; private set; }
    public TimeSpan? DuracionJornada { get; private set; }
    public bool AperturaAutomatica { get; private set; }
    public bool CierreAutomatico { get; private set; }
    public DateTimeOffset ActualizadaEnUtc { get; private set; }

    public void Actualizar(TimeOnly horaApertura, TimeOnly horaCierre, TimeSpan? duracionJornada, bool aperturaAutomatica, bool cierreAutomatico)
    {
        if ((horaApertura == horaCierre && duracionJornada is null && !cierreAutomatico) ||
            (duracionJornada.HasValue && (duracionJornada.Value <= TimeSpan.Zero || duracionJornada.Value > TimeSpan.FromHours(24))))
        {
            throw new DomainException("La hora de apertura y cierre no pueden ser iguales.");
        }
        HoraApertura = horaApertura;
        HoraCierre = horaCierre;
        DuracionJornada = duracionJornada;
        AperturaAutomatica = aperturaAutomatica;
        CierreAutomatico = cierreAutomatico;
        ActualizadaEnUtc = DateTimeOffset.UtcNow;
    }
}
