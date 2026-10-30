using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistLubri.Api.Contracts.Compras;
using SistLubri.Application.Compras;
using SistLubri.Domain.Compras;

namespace SistLubri.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/compras")]
public sealed class ComprasController(CompraService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CompraResumenDto>> Search(
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, [FromQuery] Guid? proveedorId,
        [FromQuery] EstadoRecepcionCompra? estadoRecepcion, [FromQuery] EstadoPagoCompra? estadoPago,
        CancellationToken cancellationToken) =>
        service.SearchAsync(desde, hasta, proveedorId, estadoRecepcion, estadoPago, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<CompraDetalleDto> Get(Guid id, CancellationToken cancellationToken) =>
        service.GetByIdAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<CompraDetalleDto>> Create(CrearCompraRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(new CrearCompraCommand(request.ProveedorId, request.Fecha,
            request.Observaciones, request.Detalles.Select(item => new DetalleCompraCommand(
                item.ProductoId, item.Cantidad, item.CostoUnitarioEstimado)).ToArray()), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public Task<CompraDetalleDto> Update(Guid id, ActualizarCompraRequest request, CancellationToken cancellationToken) =>
        service.UpdateAsync(id, new ActualizarCompraCommand(request.ProveedorId, request.Fecha,
            request.Observaciones, request.Detalles.Select(item => new DetalleCompraCommand(
                item.ProductoId, item.Cantidad, item.CostoUnitarioEstimado)).ToArray(), request.Version), cancellationToken);

    [HttpPost("{id:guid}/recepciones")]
    public Task<CompraDetalleDto> Receive(Guid id, RegistrarRecepcionCompraRequest request, CancellationToken cancellationToken) =>
        service.RegisterReceiptAsync(id, new RegistrarRecepcionCompraCommand(request.Fecha, request.Remito,
            request.Observaciones, request.Lineas.Select(item => new LineaRecepcionCompraCommand(
                item.DetalleCompraId, item.CantidadAceptada, item.CantidadDanada,
                item.CostoUnitario, item.Observaciones)).ToArray()), cancellationToken);

    [HttpPost("{id:guid}/facturas")]
    public Task<CompraDetalleDto> RegisterInvoice(Guid id, RegistrarFacturaCompraRequest request,
        CancellationToken cancellationToken) =>
        service.RegisterInvoiceAsync(id, new RegistrarFacturaCompraCommand(request.Numero, request.Fecha,
            request.FechaVencimiento, request.ImporteTotal, request.Observaciones, request.Version), cancellationToken);

    [HttpPost("{id:guid}/facturas/{facturaId:guid}/pagos")]
    public Task<CompraDetalleDto> RegisterPayment(Guid id, Guid facturaId, RegistrarPagoCompraRequest request,
        CancellationToken cancellationToken) =>
        service.RegisterPaymentAsync(id, facturaId, new RegistrarPagoCompraCommand(request.Fecha,
            request.MedioPagoId, request.Importe, request.Referencia, request.Observacion, request.Version), cancellationToken);

    [HttpPost("{id:guid}/cerrar-recepcion")]
    public Task<CompraDetalleDto> CloseReceiving(Guid id, CerrarRecepcionCompraRequest request,
        CancellationToken cancellationToken) => service.CloseReceivingAsync(id, request.Motivo, request.Version, cancellationToken);

    [HttpPost("{id:guid}/anular")]
    public Task<CompraDetalleDto> Cancel(Guid id, AnularCompraRequest request, CancellationToken cancellationToken) =>
        service.CancelAsync(id, request.Motivo, request.Version, cancellationToken);
}
