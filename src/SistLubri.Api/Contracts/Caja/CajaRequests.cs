using System.ComponentModel.DataAnnotations;
using SistLubri.Domain.Caja;

namespace SistLubri.Api.Contracts.Caja;

public sealed record AbrirCajaRequest([Range(0, 100000000)] decimal? SaldoInicial = null, DateOnly? Fecha = null);
public sealed record MovimientoCajaRequest(TipoMovimientoCaja Tipo, bool EsIngreso, [Range(0.01, 100000000)] decimal Importe, Guid? MedioPagoId, string? Referencia, string? Motivo);
public sealed record VerificarCajaRequest([Range(0, 100000000)] decimal SaldoContado, string? Observacion);
public sealed record ReabrirCajaRequest([Required, StringLength(500, MinimumLength = 3)] string Motivo);
public sealed record ConfigurarCajaRequest(TimeOnly HoraApertura, TimeOnly HoraCierre, TimeSpan? DuracionJornada, bool AperturaAutomatica, bool CierreAutomatico);
public sealed record ConfigurarMedioPagoRequest([Required, StringLength(60, MinimumLength = 2)] string Nombre, bool RequiereReferencia, bool AfectaEfectivo, bool Activo);
