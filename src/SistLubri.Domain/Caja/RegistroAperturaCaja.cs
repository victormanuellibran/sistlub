using SistLubri.Domain.Common;

namespace SistLubri.Domain.Caja;

public sealed class RegistroAperturaCaja : Entity
{
    private RegistroAperturaCaja() { }
    internal RegistroAperturaCaja(Guid jornadaCajaId, int numero, DateTimeOffset fechaUtc, Guid usuarioId, decimal saldoInicial)
    {
        if (jornadaCajaId == Guid.Empty || usuarioId == Guid.Empty || numero < 1 || saldoInicial < 0)
            throw new DomainException("Los datos de apertura no son validos.");
        JornadaCajaId = jornadaCajaId;
        Numero = numero;
        FechaUtc = fechaUtc;
        UsuarioId = usuarioId;
        SaldoInicial = decimal.Round(saldoInicial, 2);
    }
    public Guid JornadaCajaId { get; private set; }
    public JornadaCaja JornadaCaja { get; private init; } = null!;
    public int Numero { get; private init; }
    public DateTimeOffset FechaUtc { get; private init; }
    public Guid UsuarioId { get; private init; }
    public decimal SaldoInicial { get; private init; }
}
