using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistLubri.Api.Auth;
using SistLubri.Api.Contracts.Gastos;
using SistLubri.Application.Gastos;
using SistLubri.Domain.Gastos;

namespace SistLubri.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/gastos")]
public sealed class GastosController(GastoService service) : ControllerBase
{
    [HttpGet("categorias")]
    public ActionResult<IReadOnlyList<CategoriaGastoDto>> Categories() => Ok(GastoService.Categorias);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GastoResumenDto>>> Search(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] DateOnly? vencimientoDesde,
        [FromQuery] DateOnly? vencimientoHasta,
        [FromQuery] CategoriaGasto? categoria,
        [FromQuery] EstadoGasto? estado,
        [FromQuery] SituacionGasto? situacion,
        [FromQuery] Guid? medioPagoId,
        [FromQuery] Guid? proveedorId,
        [FromQuery] string? entidad,
        [FromQuery] decimal? importeMinimo,
        [FromQuery] decimal? importeMaximo,
        [FromQuery] Guid? usuarioId,
        [FromQuery] string? buscar,
        CancellationToken cancellationToken) =>
        Ok(await service.SearchAsync(new FiltroGastos(desde, hasta, vencimientoDesde, vencimientoHasta,
            categoria, estado, situacion, medioPagoId, proveedorId, entidad, importeMinimo,
            importeMaximo, usuarioId, buscar), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<GastoDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GastoDetalleDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<GastoDetalleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GastoDetalleDto>> Create(CrearGastoRequest request, CancellationToken cancellationToken)
    {
        var gasto = await service.CreateAsync(new CrearGastoCommand(
            request.Categoria, request.CategoriaDetalle, request.Descripcion, request.Fecha,
            request.ImporteTotal, request.EntidadRelacionada, request.ProveedorId,
            request.FechaVencimiento, request.VencimientoEstimado, request.ReferenciaComprobante,
            request.Observaciones, request.FechaPagoInicial,
            (request.PagosIniciales ?? []).Select(Map).ToArray(), request.EsCompraMercaderia), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = gasto.Id }, gasto);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<GastoDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GastoDetalleDto>> Update(
        Guid id,
        ActualizarGastoRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.UpdateAsync(id, new ActualizarGastoCommand(
            request.Categoria, request.CategoriaDetalle, request.Descripcion, request.Fecha,
            request.ImporteTotal, request.EntidadRelacionada, request.ProveedorId,
            request.FechaVencimiento, request.VencimientoEstimado, request.ReferenciaComprobante,
            request.Observaciones, request.Version), cancellationToken));

    [HttpPost("{id:guid}/pagos")]
    [ProducesResponseType<GastoDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GastoDetalleDto>> RegisterPayment(
        Guid id,
        RegistrarPagosGastoRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.RegisterPaymentAsync(id, new RegistrarPagosGastoCommand(
            request.Fecha, request.Pagos.Select(Map).ToArray()), cancellationToken));

    [HttpPost("{id:guid}/anular"), Authorize(Roles = Roles.Administrator)]
    [ProducesResponseType<GastoDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GastoDetalleDto>> Cancel(
        Guid id,
        AnularGastoRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.CancelAsync(id, new AnularGastoCommand(
            request.Motivo, request.FechaReintegro,
            (request.Reintegros ?? []).Select(Map).ToArray()), cancellationToken));

    private static PagoGastoCommand Map(PagoGastoRequest request) =>
        new(request.MedioPagoId, request.Importe, request.Referencia, request.Observacion);
}
