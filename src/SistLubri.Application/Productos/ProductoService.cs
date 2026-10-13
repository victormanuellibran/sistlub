using SistLubri.Application.Common.Exceptions;
using SistLubri.Application.Common.Persistence;
using SistLubri.Domain.Productos;

namespace SistLubri.Application.Productos;

public sealed class ProductoService(
    IProductoRepository productoRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<ProductoDetalleDto> CreateAsync(
        CrearProductoCommand command,
        CancellationToken cancellationToken = default)
    {
        var codigo = Producto.NormalizarCodigo(command.Codigo);
        if (await productoRepository.ExistsCodeAsync(codigo, cancellationToken: cancellationToken))
        {
            throw new ConflictException("Ya existe un producto registrado con ese codigo.");
        }

        var producto = new Producto(
            codigo,
            command.Nombre,
            command.Marca,
            command.Categoria,
            command.Presentacion,
            command.UnidadMedida,
            command.PrecioVenta,
            command.CostoCompra,
            command.EsFraccionado,
            command.StockInicial,
            command.StockMinimo);

        productoRepository.Add(producto);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetalle(producto);
    }

    public async Task<ProductoDetalleDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var producto = await GetProductoAsync(id, cancellationToken);
        return MapDetalle(producto);
    }

    public async Task<IReadOnlyList<ProductoResumenDto>> SearchAsync(
        string? search,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var productos = await productoRepository.SearchAsync(search, includeInactive, cancellationToken);
        return productos.Select(MapResumen).ToArray();
    }

    public async Task<IReadOnlyList<ProductoResumenDto>> GetAlertsAsync(
        CancellationToken cancellationToken = default)
    {
        var productos = await productoRepository.GetAlertsAsync(cancellationToken);
        return productos.Select(MapResumen).ToArray();
    }

    public async Task<ProductoDetalleDto> UpdateAsync(
        Guid id,
        ActualizarProductoCommand command,
        CancellationToken cancellationToken = default)
    {
        var producto = await GetProductoAsync(id, cancellationToken);
        var codigo = Producto.NormalizarCodigo(command.Codigo);

        if (await productoRepository.ExistsCodeAsync(codigo, id, cancellationToken))
        {
            throw new ConflictException("Ya existe otro producto registrado con ese codigo.");
        }

        producto.ActualizarCodigo(codigo);
        producto.ActualizarDatos(
            command.Nombre,
            command.Marca,
            command.Categoria,
            command.Presentacion,
            command.UnidadMedida,
            command.PrecioVenta,
            command.CostoCompra,
            command.EsFraccionado);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetalle(producto);
    }

    public async Task<ProductoDetalleDto> ChangeStatusAsync(
        Guid id,
        bool activo,
        CancellationToken cancellationToken = default)
    {
        var producto = await GetProductoAsync(id, cancellationToken);
        producto.CambiarEstado(activo);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetalle(producto);
    }

    public async Task<ProductoDetalleDto> SetMinimumStockAsync(
        Guid id,
        decimal stockMinimo,
        CancellationToken cancellationToken = default)
    {
        var producto = await GetProductoAsync(id, cancellationToken);
        if (!producto.Activo)
        {
            throw new ConflictException("No se puede configurar el stock minimo de un producto inactivo.");
        }

        producto.ConfigurarStockMinimo(stockMinimo);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetalle(producto);
    }

    public async Task<MovimientoInventarioDto> RegisterMovementAsync(
        Guid id,
        RegistrarMovimientoInventarioCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Tipo == TipoMovimientoInventario.CargaInicial)
        {
            throw new ConflictException(
                "La carga inicial solo puede registrarse durante el alta del producto.");
        }

        var producto = await GetProductoAsync(id, cancellationToken);
        var movimiento = producto.RegistrarMovimiento(
            command.Tipo,
            command.Cantidad,
            command.Motivo,
            command.ReferenciaId);

        productoRepository.AddMovement(movimiento);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapMovimiento(movimiento);
    }

    public async Task<IReadOnlyList<MovimientoInventarioDto>> GetMovementsAsync(
        Guid id,
        TipoMovimientoInventario? tipo,
        CancellationToken cancellationToken = default)
    {
        _ = await GetProductoAsync(id, cancellationToken);
        var movimientos = await productoRepository.GetMovementsAsync(id, tipo, cancellationToken);
        return movimientos.Select(MapMovimiento).ToArray();
    }

    private async Task<Producto> GetProductoAsync(Guid id, CancellationToken cancellationToken)
    {
        return await productoRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("No se encontro el producto solicitado.");
    }

    private static ProductoResumenDto MapResumen(Producto producto)
    {
        return new ProductoResumenDto(
            producto.Id,
            producto.Codigo,
            producto.Nombre,
            producto.Marca,
            producto.Categoria,
            producto.Presentacion,
            producto.UnidadMedida,
            producto.PrecioVenta,
            producto.StockActual,
            producto.StockMinimo,
            producto.EstadoInventario,
            producto.EsFraccionado,
            producto.Activo);
    }

    private static ProductoDetalleDto MapDetalle(Producto producto)
    {
        return new ProductoDetalleDto(
            producto.Id,
            producto.Codigo,
            producto.Nombre,
            producto.Marca,
            producto.Categoria,
            producto.Presentacion,
            producto.UnidadMedida,
            producto.PrecioVenta,
            producto.CostoCompra,
            producto.StockActual,
            producto.StockMinimo,
            producto.EstadoInventario,
            producto.EsFraccionado,
            producto.Activo,
            producto.CreadoEnUtc,
            producto.ActualizadoEnUtc);
    }

    private static MovimientoInventarioDto MapMovimiento(MovimientoInventario movimiento)
    {
        return new MovimientoInventarioDto(
            movimiento.Id,
            movimiento.ProductoId,
            movimiento.Tipo,
            movimiento.Cantidad,
            movimiento.StockAnterior,
            movimiento.StockResultante,
            movimiento.Motivo,
            movimiento.ReferenciaId,
            movimiento.FechaUtc);
    }
}
