using Microsoft.AspNetCore.Mvc;
using SistLubri.Api.Contracts.Vehiculos;
using SistLubri.Application.Vehiculos;

namespace SistLubri.Api.Controllers;

[ApiController]
[Route("api/vehiculos")]
public sealed class VehiculosController(VehiculoService vehiculoService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<VehiculoDetalleDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VehiculoDetalleDto>>> Search(
        [FromQuery] string? buscar,
        CancellationToken cancellationToken)
    {
        return Ok(await vehiculoService.SearchAsync(buscar, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<VehiculoDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehiculoDetalleDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await vehiculoService.GetByIdAsync(id, cancellationToken));
    }

    [HttpGet("patente/{patente}")]
    [ProducesResponseType<VehiculoDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehiculoDetalleDto>> GetByPatente(
        string patente,
        CancellationToken cancellationToken)
    {
        return Ok(await vehiculoService.GetByPatenteAsync(patente, cancellationToken));
    }

    [HttpPost("/api/clientes/{clienteId:guid}/vehiculos")]
    [ProducesResponseType<VehiculoDetalleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehiculoDetalleDto>> Create(
        Guid clienteId,
        CrearVehiculoRequest request,
        CancellationToken cancellationToken)
    {
        var vehiculo = await vehiculoService.CreateAsync(
            clienteId,
            new CrearVehiculoCommand(
                request.Patente,
                request.Marca,
                request.Modelo,
                request.Anio,
                request.KilometrajeActual,
                request.ProximoCambioKilometraje,
                request.Observaciones),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { vehiculo.Id }, vehiculo);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<VehiculoDetalleDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<VehiculoDetalleDto>> Update(
        Guid id,
        ActualizarVehiculoRequest request,
        CancellationToken cancellationToken)
    {
        var vehiculo = await vehiculoService.UpdateAsync(
            id,
            new ActualizarVehiculoCommand(
                request.Marca,
                request.Modelo,
                request.Anio,
                request.KilometrajeActual,
                request.ProximoCambioKilometraje,
                request.Observaciones),
            cancellationToken);

        return Ok(vehiculo);
    }

    [HttpPut("{id:guid}/cliente")]
    [ProducesResponseType<VehiculoDetalleDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<VehiculoDetalleDto>> ChangeOwner(
        Guid id,
        CambiarTitularRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await vehiculoService.ChangeOwnerAsync(id, request.ClienteId, cancellationToken));
    }
}
