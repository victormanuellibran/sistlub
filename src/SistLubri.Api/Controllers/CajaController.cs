using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistLubri.Api.Auth;
using SistLubri.Api.Contracts.Caja;
using SistLubri.Application.Caja;
using SistLubri.Domain.Caja;

namespace SistLubri.Api.Controllers;

[ApiController]
[Route("api/caja")]
public sealed class CajaController(CajaService service) : ControllerBase
{
    [HttpGet("actual")]
    public async Task<ActionResult<JornadaCajaDto?>> Current(CancellationToken cancellationToken) => Ok(await service.GetCurrentAsync(cancellationToken));

    [HttpGet("jornadas")]
    public async Task<ActionResult<IReadOnlyList<JornadaCajaDto>>> Search([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken cancellationToken) => Ok(await service.SearchAsync(desde, hasta, cancellationToken));

    [HttpGet("jornadas/{id:guid}")]
    public async Task<ActionResult<JornadaCajaDto>> Get(Guid id, CancellationToken cancellationToken) => Ok(await service.GetAsync(id, cancellationToken));

    [HttpPost("jornadas")]
    public async Task<ActionResult<JornadaCajaDto>> Open(AbrirCajaRequest request, CancellationToken cancellationToken)
    {
        var day = await service.OpenAsync(new AbrirCajaCommand(request.SaldoInicial), request.Fecha, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = day.Id }, day);
    }

    [HttpPost("movimientos")]
    public async Task<ActionResult<JornadaCajaDto>> Movement(MovimientoCajaRequest request, CancellationToken cancellationToken) => Ok(await service.RegisterMovementAsync(new MovimientoCajaCommand(request.Tipo, request.EsIngreso, request.Importe, request.MedioPagoId, request.Referencia, request.Motivo), cancellationToken));

    [HttpPost("jornadas/{id:guid}/cerrar")]
    public async Task<ActionResult<JornadaCajaDto>> Close(Guid id, CancellationToken cancellationToken) => Ok(await service.CloseAsync(id, cancellationToken));

    [HttpPost("jornadas/{id:guid}/verificar")]
    public async Task<ActionResult<JornadaCajaDto>> Verify(Guid id, VerificarCajaRequest request, CancellationToken cancellationToken) => Ok(await service.VerifyAsync(id, new VerificarCajaCommand(request.SaldoContado, request.Observacion), cancellationToken));

    [HttpPost("jornadas/{id:guid}/reabrir"), Authorize(Roles = Roles.Administrator)]
    public async Task<ActionResult<JornadaCajaDto>> Reopen(Guid id, ReabrirCajaRequest request, CancellationToken cancellationToken) => Ok(await service.ReopenAsync(id, new ReabrirCajaCommand(request.Motivo), cancellationToken));

    [HttpGet("medios-pago")]
    public async Task<ActionResult<IReadOnlyList<MedioPagoDto>>> PaymentMethods(CancellationToken cancellationToken) => Ok(await service.ListPaymentMethodsAsync(cancellationToken));

    [HttpPost("medios-pago"), Authorize(Roles = Roles.Administrator)]
    public async Task<ActionResult<MedioPagoDto>> CreatePaymentMethod(ConfigurarMedioPagoRequest request, CancellationToken cancellationToken) => Ok(await service.ConfigurePaymentMethodAsync(null, new ConfigurarMedioPagoCommand(request.Nombre, request.RequiereReferencia, request.AfectaEfectivo, request.Activo), cancellationToken));

    [HttpPut("medios-pago/{id:guid}"), Authorize(Roles = Roles.Administrator)]
    public async Task<ActionResult<MedioPagoDto>> UpdatePaymentMethod(Guid id, ConfigurarMedioPagoRequest request, CancellationToken cancellationToken) => Ok(await service.ConfigurePaymentMethodAsync(id, new ConfigurarMedioPagoCommand(request.Nombre, request.RequiereReferencia, request.AfectaEfectivo, request.Activo), cancellationToken));

    [HttpGet("configuracion")]
    public async Task<ActionResult<ConfiguracionCaja>> GetConfiguration(CancellationToken cancellationToken) => Ok(await service.GetConfigurationAsync(cancellationToken));

    [HttpPut("configuracion"), Authorize(Roles = Roles.Administrator)]
    public async Task<ActionResult<ConfiguracionCaja>> Configure(ConfigurarCajaRequest request, CancellationToken cancellationToken) => Ok(await service.ConfigureAsync(new ConfigurarCajaCommand(request.HoraApertura, request.HoraCierre, request.DuracionJornada, request.AperturaAutomatica, request.CierreAutomatico), cancellationToken));
}
