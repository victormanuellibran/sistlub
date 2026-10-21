using Microsoft.AspNetCore.Mvc;
using SistLubri.Api.Contracts.Clientes;
using SistLubri.Application.Clientes;

namespace SistLubri.Api.Controllers;

[ApiController]
[Route("api/clientes")]
public sealed class ClientesController(ClienteService clienteService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ClienteResumenDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClienteResumenDto>>> Search(
        [FromQuery] string? buscar,
        [FromQuery] bool incluirInactivos = false,
        CancellationToken cancellationToken = default)
    {
        var clientes = await clienteService.SearchAsync(buscar, incluirInactivos, cancellationToken);
        return Ok(clientes);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ClienteDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteDetalleDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await clienteService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<ClienteDetalleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClienteDetalleDto>> Create(
        CrearClienteRequest request,
        CancellationToken cancellationToken)
    {
        var cliente = await clienteService.CreateAsync(
            new CrearClienteCommand(
                request.Nombre,
                request.Apellido,
                request.Telefono,
                request.Email,
                request.Domicilio,
                request.Observaciones),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { cliente.Id }, cliente);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ClienteDetalleDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ClienteDetalleDto>> Update(
        Guid id,
        ActualizarClienteRequest request,
        CancellationToken cancellationToken)
    {
        var cliente = await clienteService.UpdateAsync(
            id,
            new ActualizarClienteCommand(
                request.Nombre,
                request.Apellido,
                request.Telefono,
                request.Email,
                request.Domicilio,
                request.Observaciones),
            cancellationToken);

        return Ok(cliente);
    }

    [HttpPost("{id:guid}/desactivar")]
    [ProducesResponseType<ClienteDetalleDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ClienteDetalleDto>> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await clienteService.DeactivateAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/reactivar")]
    [ProducesResponseType<ClienteDetalleDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ClienteDetalleDto>> Reactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await clienteService.ReactivateAsync(id, cancellationToken));
    }
}
