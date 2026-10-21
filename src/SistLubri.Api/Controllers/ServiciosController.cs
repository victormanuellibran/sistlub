using Microsoft.AspNetCore.Mvc;
using SistLubri.Api.Contracts.Servicios;
using SistLubri.Application.Servicios;

namespace SistLubri.Api.Controllers;

[ApiController]
[Route("api/servicios")]
public sealed class ServiciosController(ServicioVehicularService servicioService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ServicioVehicularDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServicioVehicularDto>> Create(
        CrearServicioVehicularRequest request,
        CancellationToken cancellationToken)
    {
        var servicio = await servicioService.CreateAsync(
            new CrearServicioVehicularCommand(
                request.Patente,
                request.KilometrajeIngreso,
                request.TrabajoSolicitado,
                request.InspeccionPrevia,
                request.DanosExistentes,
                request.Observaciones,
                request.ManoObra,
                (request.Detalles ?? []).Select(detalle =>
                    new CrearDetalleServicioCommand(detalle.ProductoId, detalle.Cantidad)).ToArray()),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = servicio.Id }, servicio);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ServicioVehicularDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServicioVehicularDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await servicioService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}/detalles")]
    [ProducesResponseType<ServicioVehicularDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServicioVehicularDto>> UpdateDetails(
        Guid id,
        ActualizarDetallesServicioRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await servicioService.UpdateDetailsAsync(
            id,
            (request.Detalles ?? []).Select(detalle =>
                new CrearDetalleServicioCommand(detalle.ProductoId, detalle.Cantidad)).ToArray(),
            cancellationToken));
    }

    [HttpGet("patente/{patente}")]
    [ProducesResponseType<IReadOnlyList<ServicioVehicularDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ServicioVehicularDto>>> GetHistoryByPlate(
        string patente,
        CancellationToken cancellationToken)
    {
        return Ok(await servicioService.GetHistoryByPlateAsync(patente, cancellationToken));
    }

    [HttpPost("{id:guid}/finalizar")]
    [ProducesResponseType<ServicioVehicularDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServicioVehicularDto>> Finalize(
        Guid id,
        FinalizarServicioRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await servicioService.FinalizeAsync(
            id,
            request.KilometrajeFinal,
            request.ProximoCambioKilometraje,
            cancellationToken));
    }

    [HttpPost("{id:guid}/anular")]
    [ProducesResponseType<ServicioVehicularDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServicioVehicularDto>> Cancel(
        Guid id,
        AnularServicioRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await servicioService.CancelAsync(id, request.Motivo, cancellationToken));
    }
}
