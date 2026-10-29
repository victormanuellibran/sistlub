using SistLubri.Domain.Common;

namespace SistLubri.Domain.Caja;

public sealed class RegistroCierreCaja : Entity
{
    private RegistroCierreCaja() { }

    internal RegistroCierreCaja(Guid jornadaCajaId, int numero, EstadoJornadaCaja estado,
        DateTimeOffset cerradoEnUtc, Guid cerradoPorUsuarioId, decimal efectivoEsperado,
        decimal? efectivoContado, decimal? diferencia, string? observacion, DateTimeOffset? verificadoEnUtc,
        Guid? verificadoPorUsuarioId = null)
    {
        JornadaCajaId = jornadaCajaId;
        Numero = numero;
        Estado = estado;
        CerradoEnUtc = cerradoEnUtc;
        CerradoPorUsuarioId = cerradoPorUsuarioId;
        EfectivoEsperado = efectivoEsperado;
        EfectivoContado = efectivoContado;
        Diferencia = diferencia;
        Observacion = observacion;
        VerificadoEnUtc = verificadoEnUtc;
        VerificadoPorUsuarioId = verificadoPorUsuarioId;
    }

    public Guid JornadaCajaId { get; private set; }
    public JornadaCaja JornadaCaja { get; private init; } = null!;
    public int Numero { get; private init; }
    public EstadoJornadaCaja Estado { get; private set; }
    public DateTimeOffset CerradoEnUtc { get; private init; }
    public Guid CerradoPorUsuarioId { get; private init; }
    public decimal EfectivoEsperado { get; private init; }
    public decimal? EfectivoContado { get; private set; }
    public decimal? Diferencia { get; private set; }
    public string? Observacion { get; private set; }
    public DateTimeOffset? VerificadoEnUtc { get; private set; }
    public Guid? VerificadoPorUsuarioId { get; private set; }
    public DateTimeOffset? ReabiertoEnUtc { get; private set; }
    public Guid? ReabiertoPorUsuarioId { get; private set; }
    public string? MotivoReapertura { get; private set; }
    public Guid? ProximoCierreId { get; private set; }

    internal void Verificar(decimal contado, decimal diferencia, string? observacion, DateTimeOffset fecha, Guid usuarioId)
    {
        EfectivoContado = contado;
        Diferencia = diferencia;
        Observacion = observacion;
        VerificadoEnUtc = fecha;
        VerificadoPorUsuarioId = usuarioId;
        Estado = EstadoJornadaCaja.Verificada;
    }

    internal void RegistrarReapertura(string motivo, DateTimeOffset fecha, Guid usuarioId)
    {
        ReabiertoEnUtc = fecha;
        ReabiertoPorUsuarioId = usuarioId;
        MotivoReapertura = motivo;
        Estado = EstadoJornadaCaja.Reabierta;
    }

    internal void VincularNuevoCierre(Guid cierreId)
    {
        if (cierreId == Guid.Empty) throw new DomainException("El cierre vinculado no es valido.");
        ProximoCierreId = cierreId;
    }

}
