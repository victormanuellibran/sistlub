using Microsoft.AspNetCore.Mvc;
using SistLubri.Api.Contracts.Productos;
using SistLubri.Application.Productos;
using SistLubri.Domain.Productos;

namespace SistLubri.Api.Controllers;

[ApiController]
[Route("api/productos")]
public sealed class ProductosController(ProductoService productoService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductoResumenDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductoResumenDto>>> Search(
        [FromQuery] string? buscar,
        [FromQuery] bool incluirInactivos,
        CancellationToken cancellationToken)
    {
        return Ok(await productoService.SearchAsync(buscar, incluirInactivos, cancellationToken));
    }

    [HttpGet("alertas")]
    [ProducesResponseType<IReadOnlyList<ProductoResumenDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductoResumenDto>>> GetAlerts(
        CancellationToken cancellationToken)
    {
        return Ok(await productoService.GetAlertsAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProductoDetalleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductoDetalleDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await productoService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<ProductoDetalleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductoDetalleDto>> Create(
        CrearProductoRequest request,
        CancellationToken cancellationToken)
    {
        var producto = await productoService.CreateAsync(
            new CrearProductoCommand(
                request.Codigo,
                request.Nombre,
                request.Marca,
                request.Categoria,
                request.Presentacion,
                request.UnidadMedida,
                request.PrecioVenta,
                request.CostoCompra,
                request.EsFraccionado,
                request.StockInicial,
                request.StockMinimo),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { producto.Id }, producto);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ProductoDetalleDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductoDetalleDto>> Update(
        Guid id,
        ActualizarProductoRequest request,
        CancellationToken cancellationToken)
    {
        var producto = await productoService.UpdateAsync(
            id,
            new ActualizarProductoCommand(
                request.Codigo,
                request.Nombre,
                request.Marca,
                request.Categoria,
                request.Presentacion,
                request.UnidadMedida,
                request.PrecioVenta,
                request.CostoCompra,
                request.EsFraccionado),
            cancellationToken);

        return Ok(producto);
    }

    [HttpPut("{id:guid}/estado")]
    [ProducesResponseType<ProductoDetalleDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductoDetalleDto>> ChangeStatus(
        Guid id,
        CambiarEstadoProductoRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await productoService.ChangeStatusAsync(id, request.Activo, cancellationToken));
    }

    [HttpPut("{id:guid}/stock-minimo")]
    [ProducesResponseType<ProductoDetalleDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductoDetalleDto>> SetMinimumStock(
        Guid id,
        ConfigurarStockMinimoRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await productoService.SetMinimumStockAsync(
            id,
            request.StockMinimo,
            cancellationToken));
    }

    [HttpGet("{id:guid}/movimientos")]
    [ProducesResponseType<IReadOnlyList<MovimientoInventarioDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MovimientoInventarioDto>>> GetMovements(
        Guid id,
        [FromQuery] TipoMovimientoInventario? tipo,
        CancellationToken cancellationToken)
    {
        return Ok(await productoService.GetMovementsAsync(id, tipo, cancellationToken));
    }

    [HttpGet("/api/inventario/movimientos")]
    [ProducesResponseType<IReadOnlyList<MovimientoInventarioDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MovimientoInventarioDto>>> SearchMovements(
        [FromQuery] Guid? productoId,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] TipoMovimientoInventario? tipo,
        CancellationToken cancellationToken)
    {
        return Ok(await productoService.SearchMovementsAsync(productoId, desde, hasta, tipo, cancellationToken));
    }

    [HttpPost("{id:guid}/movimientos")]
    [ProducesResponseType<MovimientoInventarioDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<MovimientoInventarioDto>> RegisterMovement(
        Guid id,
        RegistrarMovimientoInventarioRequest request,
        CancellationToken cancellationToken)
    {
        var movimiento = await productoService.RegisterMovementAsync(
            id,
            new RegistrarMovimientoInventarioCommand(
                request.Tipo,
                request.Cantidad,
                request.Motivo,
                request.ReferenciaId),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, movimiento);
    }
}
