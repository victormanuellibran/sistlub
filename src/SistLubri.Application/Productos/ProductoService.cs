using SistLubri.Application.Common.Exceptions;
using SistLubri.Application.Common.Persistence;
using SistLubri.Application.Common.Security;
using SistLubri.Domain.Productos;

namespace SistLubri.Application.Productos;

public sealed class ProductoService(
    IProductoRepository productoRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser? currentUser = null)
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
            command.StockMinimo,
            currentUser?.Id);

        productoRepository.Add(producto);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetalle(producto, producto.Movimientos.Select(item => (DateTimeOffset?)item.FechaUtc).Max());
    }

    public async Task<ProductoDetalleDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var producto = await GetProductoAsync(id, cancellationToken);
        return await MapDetalleAsync(producto, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductoResumenDto>> SearchAsync(
        string? search,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var productos = await productoRepository.SearchAsync(search, includeInactive, cancellationToken);
        var dates = await productoRepository.GetLatestMovementDatesAsync(productos.Select(item => item.Id).ToArray(), cancellationToken);
        return productos.Select(producto => MapResumen(producto, dates.GetValueOrDefault(producto.Id))).ToArray();
    }

    public async Task<IReadOnlyList<ProductoResumenDto>> GetAlertsAsync(
        CancellationToken cancellationToken = default)
    {
        var productos = await productoRepository.GetAlertsAsync(cancellationToken);
        var dates = await productoRepository.GetLatestMovementDatesAsync(productos.Select(item => item.Id).ToArray(), cancellationToken);
        return productos.Select(producto => MapResumen(producto, dates.GetValueOrDefault(producto.Id))).ToArray();
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
        return await MapDetalleAsync(producto, cancellationToken);
    }

    public async Task<ProductoDetalleDto> ChangeStatusAsync(
        Guid id,
        bool activo,
        CancellationToken cancellationToken = default)
    {
        var producto = await GetProductoAsync(id, cancellationToken);
        if (!activo && producto.StockReservado > 0)
        {
            throw new ConflictException("No se puede desactivar un producto reservado por un servicio en curso.");
        }

        producto.CambiarEstado(activo);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapDetalleAsync(producto, cancellationToken);
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

        producto.ConfigurarStockMinimo(stockMinimo, currentUser?.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapDetalleAsync(producto, cancellationToken);
    }

    public async Task<MovimientoInventarioDto> RegisterMovementAsync(
        Guid id,
        RegistrarMovimientoInventarioCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Tipo is not (TipoMovimientoInventario.AjustePositivo or TipoMovimientoInventario.AjusteNegativo))
        {
            throw new ConflictException(
                "Los movimientos de compra, venta y servicio se generan desde sus respectivos modulos. Aqui solo se permiten ajustes positivos o negativos.");
        }

        var producto = await GetProductoAsync(id, cancellationToken);
        var movimiento = producto.RegistrarMovimiento(
            command.Tipo,
            command.Cantidad,
            command.Motivo,
            command.ReferenciaId,
            currentUser?.Id);

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

    public async Task<IReadOnlyList<MovimientoInventarioDto>> SearchMovementsAsync(
        Guid? productoId,
        DateOnly? desde,
        DateOnly? hasta,
        TipoMovimientoInventario? tipo,
        CancellationToken cancellationToken = default)
    {
        if (desde.HasValue && hasta.HasValue && desde > hasta)
        {
            throw new Domain.Common.DomainException("La fecha desde no puede ser posterior a la fecha hasta.");
        }

        var movimientos = await productoRepository.SearchMovementsAsync(
            productoId, desde, hasta, tipo, cancellationToken);
        return movimientos.Select(MapMovimiento).ToArray();
    }

    private async Task<Producto> GetProductoAsync(Guid id, CancellationToken cancellationToken)
    {
        return await productoRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("No se encontro el producto solicitado.");
    }

    private async Task<ProductoDetalleDto> MapDetalleAsync(Producto producto, CancellationToken cancellationToken)
    {
        var dates = await productoRepository.GetLatestMovementDatesAsync([producto.Id], cancellationToken);
        return MapDetalle(producto, dates.GetValueOrDefault(producto.Id));
    }

    private static ProductoResumenDto MapResumen(Producto producto, DateTimeOffset? fechaUltimoMovimientoUtc)
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
            producto.StockReservado,
            producto.StockDisponible,
            producto.StockMinimo,
            producto.EstadoInventario,
            producto.EsFraccionado,
            producto.Activo,
            fechaUltimoMovimientoUtc);
    }

    private static ProductoDetalleDto MapDetalle(Producto producto, DateTimeOffset? fechaUltimoMovimientoUtc = null)
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
            producto.StockReservado,
            producto.StockDisponible,
            producto.StockMinimo,
            producto.EstadoInventario,
            producto.EsFraccionado,
            producto.Activo,
            producto.CreadoEnUtc,
            producto.ActualizadoEnUtc,
            fechaUltimoMovimientoUtc,
            producto.StockMinimoActualizadoEnUtc,
            producto.StockMinimoActualizadoPorUsuarioId);
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
            movimiento.FechaUtc,
            movimiento.UsuarioId,
            movimiento.Tipo switch
            {
                TipoMovimientoInventario.EntradaCompra => "Compra",
                TipoMovimientoInventario.Salida => "Venta o servicio",
                TipoMovimientoInventario.Entrada => "Devolucion o correccion",
                TipoMovimientoInventario.AjustePositivo or TipoMovimientoInventario.AjusteNegativo => "Ajuste",
                TipoMovimientoInventario.CargaInicial => "Carga inicial",
                TipoMovimientoInventario.DevolucionProveedor => "Devolucion a proveedor",
                _ => "Otro"
            });
    }
}
