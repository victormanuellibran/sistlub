using Microsoft.AspNetCore.Mvc;
using SistLubri.Api.Contracts.Ventas;
using SistLubri.Application.Ventas;

namespace SistLubri.Api.Controllers;

[ApiController]
[Route("api/ventas")]
public sealed class VentasController(VentaService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VentaDetalleDto>>> Search([FromQuery] DateTimeOffset? desde, [FromQuery] DateTimeOffset? hasta, [FromQuery] Guid? clienteId, [FromQuery] SistLubri.Domain.Ventas.EstadoVenta? estado, CancellationToken cancellationToken) =>
        Ok(await service.SearchAsync(desde, hasta, clienteId, estado, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VentaDetalleDto>> Get(Guid id, CancellationToken cancellationToken) => Ok(await service.GetAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<VentaDetalleDto>> Create(CrearVentaRequest request, CancellationToken cancellationToken)
    {
        var sale = await service.CrearVentaAsync(new CrearVentaCommand(request.ClienteId, request.ReferenciaFactura,
            request.DescuentoGeneral,
            request.Detalles.Select(d => new DetalleVentaCommand(d.ProductoId, d.Cantidad, d.PrecioUnitario, d.Descuento)).ToArray(),
            request.Pagos.Select(p => new PagoVentaCommand(p.MedioPagoId, p.Importe, p.Referencia, p.Observacion)).ToArray()), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = sale.Id }, sale);
    }

    [HttpPost("servicios/{servicioId:guid}/cobros")]
    public async Task<ActionResult<VentaDetalleDto>> ChargeService(Guid servicioId, CobrarServicioVentaRequest request, CancellationToken cancellationToken)
    {
        var sale = await service.CobrarServicioAsync(servicioId, new CobrarServicioCommand(request.ImporteManoObra,
            request.DescuentoGeneral,
            request.Pagos.Select(p => new PagoVentaCommand(p.MedioPagoId, p.Importe, p.Referencia, p.Observacion)).ToArray(),
            request.ReferenciaFactura), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = sale.Id }, sale);
    }

    [HttpPut("{id:guid}/pendiente")]
    public async Task<ActionResult<VentaDetalleDto>> EditPending(Guid id, EditarVentaRequest request, CancellationToken cancellationToken) =>
        Ok(await service.EditPendingAsync(id, new EditarVentaPendienteCommand(request.ImporteManoObra,
            request.DescuentoGeneral, request.ReferenciaFactura,
            request.Detalles.Select(d => new DetalleVentaCommand(d.ProductoId, d.Cantidad, d.PrecioUnitario, d.Descuento)).ToArray()), cancellationToken));

    [HttpPost("{id:guid}/devoluciones")]
    public async Task<ActionResult<VentaDetalleDto>> Return(Guid id, DevolverVentaRequest request, CancellationToken cancellationToken) =>
        Ok(await service.ReturnAsync(id, new DevolverVentaCommand(request.Motivo,
            request.Reintegros.Select(p => new PagoVentaCommand(p.MedioPagoId, p.Importe, p.Referencia, p.Observacion)).ToArray(),
            request.Detalles.Select(d => new DetalleDevolucionCommand(d.ProductoId, d.Cantidad, d.ReingresarAlInventario)).ToArray()), cancellationToken));

    [HttpPost("{id:guid}/anular")]
    public async Task<ActionResult<VentaDetalleDto>> Cancel(Guid id, [FromBody] AnularVentaRequest request, CancellationToken cancellationToken) =>
        Ok(await service.CancelAsync(id, request.Motivo, cancellationToken));
}
