using SistLubri.Domain.Caja;

namespace SistLubri.Application.Caja;

public sealed record AbrirCajaCommand(decimal? SaldoInicial);
public sealed record MovimientoCajaCommand(TipoMovimientoCaja Tipo, bool EsIngreso, decimal Importe, Guid? MedioPagoId, string? Referencia, string? Motivo);
public sealed record VerificarCajaCommand(decimal SaldoContado, string? Observacion);
public sealed record ReabrirCajaCommand(string Motivo);
public sealed record ConfigurarCajaCommand(TimeOnly HoraApertura, TimeOnly HoraCierre, TimeSpan? DuracionJornada, bool AperturaAutomatica, bool CierreAutomatico);
public sealed record ConfigurarMedioPagoCommand(string Nombre, bool RequiereReferencia, bool AfectaEfectivo, bool Activo);
public sealed record MovimientoCajaDto(Guid Id, TipoMovimientoCaja Tipo, bool EsIngreso, decimal Importe, Guid UsuarioId, Guid? MedioPagoId, Guid? VentaId, string? Referencia, string? Motivo, DateTimeOffset FechaUtc, bool AfectaEfectivo, Guid? GastoId, Guid? PagoGastoId, Guid? ReintegroGastoId);
public sealed record JornadaCajaDto(Guid Id, DateOnly Fecha, decimal SaldoInicial, EstadoJornadaCaja Estado, decimal TotalIngresos, decimal TotalEgresos, decimal SaldoEsperado, decimal EfectivoEsperado, decimal? SaldoContado, decimal? Diferencia, DateTimeOffset AbiertaEnUtc, Guid AbiertaPorUsuarioId, DateTimeOffset? CerradaEnUtc, Guid? CerradaPorUsuarioId, DateTimeOffset? VerificadaEnUtc, Guid? VerificadaPorUsuarioId, string? ObservacionVerificacion, DateTimeOffset? ReabiertaEnUtc, Guid? ReabiertaPorUsuarioId, string? MotivoReapertura, int NumeroReaperturas, IReadOnlyList<MovimientoCajaDto> Movimientos);
public sealed record MedioPagoDto(Guid Id, string Nombre, bool RequiereReferencia, bool AfectaEfectivo, bool Activo);
