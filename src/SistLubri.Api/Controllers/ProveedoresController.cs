using Microsoft.AspNetCore.Mvc;
using SistLubri.Api.Contracts.Proveedores;
using SistLubri.Application.Proveedores;

namespace SistLubri.Api.Controllers;

[ApiController]
[Route("api/proveedores")]
public sealed class ProveedoresController(ProveedorService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProveedorResumenDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProveedorResumenDto>>> Search(
        [FromQuery] string? buscar,
        [FromQuery] bool incluirInactivos = false,
        CancellationToken cancellationToken = default) =>
        Ok(await service.SearchAsync(buscar, incluirInactivos, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProveedorDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProveedorDetalleDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<ProveedorDetalleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProveedorDetalleDto>> Create(
        CrearProveedorRequest request,
        CancellationToken cancellationToken)
    {
        var proveedor = await service.CreateAsync(new CrearProveedorCommand(
            request.RazonSocial, request.Cuit, request.ContactoNombre, request.Telefono, request.Email,
            request.Direccion, request.MarcasYProductos, request.CondicionPagoHabitual,
            request.PlazoPagoHabitualDias, request.PeriodoPagoHabitual, request.Observaciones,
            request.ConfirmarPosiblesDuplicados), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = proveedor.Id }, proveedor);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ProveedorDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProveedorDetalleDto>> Update(
        Guid id,
        ActualizarProveedorRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.UpdateAsync(id, new ActualizarProveedorCommand(
            request.RazonSocial, request.Cuit, request.ContactoNombre, request.Telefono, request.Email,
            request.Direccion, request.MarcasYProductos, request.CondicionPagoHabitual,
            request.PlazoPagoHabitualDias, request.PeriodoPagoHabitual, request.Observaciones,
            request.ConfirmarPosiblesDuplicados), cancellationToken));

    [HttpPost("{id:guid}/desactivar")]
    [ProducesResponseType<ProveedorDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProveedorDetalleDto>> Deactivate(
        Guid id,
        DesactivarProveedorRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.DeactivateAsync(id, new DesactivarProveedorCommand(request.Motivo), cancellationToken));

    [HttpPost("{id:guid}/reactivar")]
    [ProducesResponseType<ProveedorDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProveedorDetalleDto>> Reactivate(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.ReactivateAsync(id, cancellationToken));
}
