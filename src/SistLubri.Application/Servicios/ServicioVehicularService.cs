using SistLubri.Application.Common.Exceptions;
using SistLubri.Application.Common.Persistence;
using SistLubri.Application.Common.Security;
using SistLubri.Application.Clientes;
using SistLubri.Application.Productos;
using SistLubri.Application.Vehiculos;
using SistLubri.Application.Ventas;
using SistLubri.Domain.Productos;
using SistLubri.Domain.Servicios;
using SistLubri.Domain.Ventas;

namespace SistLubri.Application.Servicios;

public sealed class ServicioVehicularService(
    IServicioVehicularRepository servicioRepository,
    IVehiculoRepository vehiculoRepository,
    IClienteRepository clienteRepository,
    IProductoRepository productoRepository,
    IVentaRepository ventaRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
{
    public async Task<ServicioVehicularDto> CreateAsync(
        CrearServicioVehicularCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var patente = SistLubri.Domain.Vehiculos.Vehiculo.NormalizarPatente(command.Patente);
        var vehiculo = await vehiculoRepository.GetByPatenteAsync(patente, cancellationToken)
            ?? throw new NotFoundException("No se encontro un vehiculo con esa patente.");
        var cliente = await clienteRepository.GetByIdAsync(vehiculo.ClienteId, cancellationToken)
            ?? throw new NotFoundException("No se encontro el cliente asociado al vehiculo.");

        if (!cliente.Activo)
        {
            throw new ConflictException("No se puede iniciar un servicio para un cliente inactivo.");
        }

        if (vehiculo.KilometrajeActual.HasValue && command.KilometrajeIngreso < vehiculo.KilometrajeActual)
        {
            throw new ConflictException("El kilometraje de ingreso no puede ser menor al ultimo registrado para esta patente.");
        }

        ValidateUniqueProducts(command.Detalles);
        var products = await LoadRequestedProductsAsync(command.Detalles, cancellationToken);
        var service = new ServicioVehicular(
            vehiculo.Id,
            vehiculo.ClienteId,
            vehiculo.Patente,
            $"{cliente.Nombre} {cliente.Apellido}",
            command.KilometrajeIngreso,
            command.TrabajoSolicitado,
            command.InspeccionPrevia,
            command.DanosExistentes,
            command.Observaciones,
            command.ManoObra,
            currentUser.Id);

        foreach (var (product, detail) in products)
        {
            if (decimal.Round(detail.Cantidad, 3) <= 0 || decimal.Round(detail.Cantidad, 3) > product.StockDisponible)
            {
                throw new ConflictException($"Stock insuficiente para reservar '{product.Nombre}'. Disponible: {product.StockDisponible}.");
            }
        }

        foreach (var (product, detail) in products)
        {
            Reserve(product, detail.Cantidad);
            service.AgregarOActualizarDetalle(
                product.Id, product.Codigo, product.Nombre, product.UnidadMedida,
                product.EsFraccionado, detail.Cantidad, product.PrecioVenta, product.CostoCompra, currentUser.Id);
        }

        servicioRepository.Add(service);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(service);
    }

    public async Task<ServicioVehicularDto> UpdateDetailsAsync(
        Guid id,
        ActualizarServicioVehicularCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var service = await GetServicioAsync(id, cancellationToken);
        ValidateUniqueProducts(command.Detalles);

        var previous = service.Detalles.ToDictionary(detail => detail.ProductoId, detail => detail.Cantidad);
        var requested = await LoadRequestedProductsAsync(command.Detalles, cancellationToken);
        var products = requested.ToDictionary(item => item.Product.Id, item => item);
        foreach (var item in requested)
        {
            if (decimal.Round(item.Detail.Cantidad, 3) <= 0)
            {
                throw new SistLubri.Domain.Common.DomainException("La cantidad reservada debe ser mayor que cero.");
            }
            if (!previous.ContainsKey(item.Product.Id) && item.Detail.Cantidad > item.Product.StockDisponible)
            {
                throw new ConflictException($"Stock insuficiente para reservar '{item.Product.Nombre}'. Disponible: {item.Product.StockDisponible}.");
            }
        }

        foreach (var productId in previous.Keys.Except(products.Keys))
        {
            var product = await productoRepository.GetByIdAsync(productId, cancellationToken)
                ?? throw new NotFoundException("No se encontro un producto previamente reservado.");
            products.Add(productId, (product, new CrearDetalleServicioCommand(productId, 0)));
        }

        foreach (var (productId, previousQuantity) in previous)
        {
            var (product, detail) = products[productId];
            var delta = detail.Cantidad - previousQuantity;
            if (delta > 0 && decimal.Round(delta, 3) > product.StockDisponible)
            {
                throw new ConflictException($"Stock insuficiente para reservar '{product.Nombre}'. Disponible: {product.StockDisponible}.");
            }
        }

        foreach (var (productId, previousQuantity) in previous)
        {
            var (product, detail) = products[productId];
            var delta = detail.Cantidad - previousQuantity;
            if (delta > 0)
            {
                Reserve(product, delta);
            }
            else if (delta < 0)
            {
                product.LiberarReserva(-delta);
            }
        }

        foreach (var item in requested.Where(item => !previous.ContainsKey(item.Product.Id)))
        {
            Reserve(item.Product, item.Detail.Cantidad);
        }

        foreach (var item in requested)
        {
            service.AgregarOActualizarDetalle(
                item.Product.Id, item.Product.Codigo, item.Product.Nombre, item.Product.UnidadMedida,
                item.Product.EsFraccionado, item.Detail.Cantidad, item.Product.PrecioVenta,
                item.Product.CostoCompra, currentUser.Id);
        }
        service.EliminarDetallesExcepto(products.Where(pair => pair.Value.Detail.Cantidad > 0)
            .Select(pair => pair.Key).ToHashSet());

        service.ActualizarDatosEnCurso(
            command.TrabajoSolicitado ?? service.TrabajoSolicitado,
            command.Observaciones ?? service.Observaciones,
            command.ManoObra ?? service.ManoObra,
            currentUser.Id);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(service);
    }

    public async Task<ServicioVehicularDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Map(await GetServicioAsync(id, cancellationToken));

    public async Task<IReadOnlyList<ServicioVehicularDto>> GetInProgressAsync(CancellationToken cancellationToken = default)
    {
        var services = await servicioRepository.GetInProgressAsync(cancellationToken);
        return services.Select(Map).ToArray();
    }

    public async Task<IReadOnlyList<ServicioVehicularDto>> GetHistoryByPlateAsync(
        string patente,
        CancellationToken cancellationToken = default)
    {
        var normalizedPlate = SistLubri.Domain.Vehiculos.Vehiculo.NormalizarPatente(patente);
        var vehicle = await vehiculoRepository.GetByPatenteAsync(normalizedPlate, cancellationToken)
            ?? throw new NotFoundException("No se encontro un vehiculo con esa patente.");
        var history = await servicioRepository.GetByVehiculoIdAsync(vehicle.Id, cancellationToken);
        return history.Select(Map).ToArray();
    }

    public Task<ServicioVehicularDto> FinalizeAsync(
        Guid id,
        CerrarServicioVehicularCommand command,
        CancellationToken cancellationToken = default) =>
        CloseAsync(id, command, finalize: true, cancellationToken);

    public Task<ServicioVehicularDto> CancelAsync(
        Guid id,
        CerrarServicioVehicularCommand command,
        CancellationToken cancellationToken = default) =>
        CloseAsync(id, command, finalize: false, cancellationToken);

    private async Task<ServicioVehicularDto> CloseAsync(
        Guid id,
        CerrarServicioVehicularCommand command,
        bool finalize,
        CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        var service = await GetServicioAsync(id, cancellationToken);
        if (!finalize && (string.IsNullOrWhiteSpace(command.MotivoAnulacion) || command.MotivoAnulacion.Trim().Length > 500))
        {
            throw new SistLubri.Domain.Common.DomainException("El motivo de anulacion es obligatorio y no puede superar 500 caracteres.");
        }

        if (await ventaRepository.ExistsByServiceIdAsync(service.Id, cancellationToken))
        {
            throw new ConflictException("El servicio ya tiene una venta asociada; no se puede volver a cerrar ni duplicar su consumo.");
        }

        ArgumentNullException.ThrowIfNull(command.ProductosUtilizados);
        ValidateUniqueConsumptions(command.ProductosUtilizados);
        var reserved = service.Detalles.ToDictionary(detail => detail.ProductoId, detail => detail.Cantidad);
        if (command.ProductosUtilizados.Count != reserved.Count ||
            command.ProductosUtilizados.Any(item => !reserved.ContainsKey(item.ProductoId)))
        {
            throw new SistLubri.Domain.Common.DomainException(
                "El checklist debe incluir exactamente los productos reservados para el servicio.");
        }

        if (command.ProductosUtilizados.Any(item => item.CantidadUtilizada < 0))
        {
            throw new SistLubri.Domain.Common.DomainException("La cantidad utilizada no puede ser negativa.");
        }

        var used = command.ProductosUtilizados.ToDictionary(item => item.ProductoId, item => decimal.Round(item.CantidadUtilizada, 3));
        foreach (var (productId, usedQuantity) in used)
        {
            if (usedQuantity < 0 || usedQuantity > reserved[productId])
            {
                throw new SistLubri.Domain.Common.DomainException(
                    "La cantidad utilizada debe estar entre cero y la cantidad reservada.");
            }
        }

        var vehicle = finalize
            ? await vehiculoRepository.GetByIdAsync(service.VehiculoId, cancellationToken)
                ?? throw new NotFoundException("No se encontro el vehiculo asociado al servicio.")
            : null;
        if (finalize)
        {
            ValidateFinalMileage(service, vehicle!, command.KilometrajeFinal, command.ProximoCambioKilometraje);
        }

        foreach (var detail in service.Detalles)
        {
            var product = await productoRepository.GetByIdAsync(detail.ProductoId, cancellationToken)
                ?? throw new NotFoundException($"No se encontro el producto '{detail.NombreProducto}'.");
            if (!product.Activo)
            {
                throw new ConflictException($"El producto '{product.Nombre}' esta inactivo y no se puede cerrar el servicio.");
            }
            if (product.UnidadMedida != detail.UnidadMedida || product.EsFraccionado != detail.EsFraccionado)
            {
                throw new ConflictException($"La unidad o modalidad del producto '{product.Nombre}' cambio desde que se agrego al servicio.");
            }

            var quantityUsed = used[detail.ProductoId];
            product.LiberarReserva(detail.Cantidad);
            if (quantityUsed > 0)
            {
                var movement = product.RegistrarMovimiento(
                    TipoMovimientoInventario.Salida,
                    quantityUsed,
                    $"Consumo del servicio {service.Id}",
                    service.Id,
                    currentUser.Id);
                productoRepository.AddMovement(movement);
            }
        }

        service.ConfirmarConsumo(used, currentUser.Id);
        if (finalize)
        {
            var nextMileage = command.ProximoCambioKilometraje ?? vehicle!.ProximoCambioKilometraje;
            if (nextMileage.HasValue && command.KilometrajeFinal.HasValue &&
                nextMileage.Value <= command.KilometrajeFinal.Value)
            {
                nextMileage = null;
            }
            vehicle!.ActualizarDatos(vehicle.Marca, vehicle.Modelo, vehicle.Anio,
                command.KilometrajeFinal!.Value, nextMileage, vehicle.Observaciones);
            service.Finalizar(currentUser.Id);
        }
        else
        {
            service.Anular(command.MotivoAnulacion ?? string.Empty, currentUser.Id);
        }

        if (service.Detalles.Count > 0 || (finalize && service.ManoObra > 0))
        {
            var sale = Venta.CrearVentaDeServicio(
                service.Id,
                service.ClienteId,
                service.ClienteNombre,
                service.Patente,
                finalize ? service.ManoObra : 0,
                null,
                currentUser.Id,
                service.Detalles.Select(detail => new DetalleVentaInicial(
                    detail.ProductoId, detail.CodigoProducto, detail.NombreProducto,
                    detail.UnidadMedida.ToString(), detail.UnidadMedida, detail.EsFraccionado,
                    detail.Cantidad, detail.PrecioUnitario)).ToArray());
            ventaRepository.Add(sale);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(service);
    }

    private static void ValidateFinalMileage(
        ServicioVehicular service,
        SistLubri.Domain.Vehiculos.Vehiculo vehicle,
        int? finalMileage,
        int? nextChangeMileage)
    {
        if (vehicle.Patente != service.Patente)
        {
            throw new ConflictException("La patente del vehiculo cambio desde que se inicio el servicio.");
        }

        if (!finalMileage.HasValue || finalMileage.Value < service.KilometrajeIngreso)
        {
            throw new ConflictException("El kilometraje final no puede ser menor al kilometraje de ingreso.");
        }

        if (vehicle.KilometrajeActual.HasValue && finalMileage.Value < vehicle.KilometrajeActual.Value)
        {
            throw new ConflictException("El kilometraje final es menor al ultimo registrado para esta patente.");
        }

        if (nextChangeMileage.HasValue && nextChangeMileage.Value <= finalMileage.Value)
        {
            throw new ConflictException("El kilometraje del proximo cambio debe ser mayor al kilometraje final.");
        }
    }

    private async Task<List<(Producto Product, CrearDetalleServicioCommand Detail)>> LoadRequestedProductsAsync(
        IReadOnlyList<CrearDetalleServicioCommand> details,
        CancellationToken cancellationToken)
    {
        var products = new List<(Producto Product, CrearDetalleServicioCommand Detail)>();
        foreach (var detail in details)
        {
            var product = await productoRepository.GetByIdAsync(detail.ProductoId, cancellationToken)
                ?? throw new NotFoundException($"No se encontro el producto {detail.ProductoId}.");
            if (!product.Activo)
            {
                throw new ConflictException($"El producto '{product.Nombre}' esta inactivo.");
            }

            products.Add((product, detail));
        }

        return products;
    }

    private static void Reserve(Producto product, decimal quantity)
    {
        try
        {
            product.ReservarStock(quantity);
        }
        catch (SistLubri.Domain.Common.DomainException exception)
        {
            throw new ConflictException(exception.Message);
        }
    }

    private static void ValidateUniqueProducts(IReadOnlyList<CrearDetalleServicioCommand> details)
    {
        if (details.Select(detail => detail.ProductoId).Distinct().Count() != details.Count)
        {
            throw new SistLubri.Domain.Common.DomainException(
                "Cada producto debe aparecer una sola vez; consolide las cantidades.");
        }
    }

    private static void ValidateUniqueConsumptions(IReadOnlyList<ConfirmarConsumoServicioCommand> consumptions)
    {
        if (consumptions.Select(item => item.ProductoId).Distinct().Count() != consumptions.Count)
        {
            throw new SistLubri.Domain.Common.DomainException("Cada producto debe aparecer una sola vez en el checklist.");
        }
    }

    private async Task<ServicioVehicular> GetServicioAsync(Guid id, CancellationToken cancellationToken) =>
        await servicioRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("No se encontro el servicio solicitado.");

    private static ServicioVehicularDto Map(ServicioVehicular service) => new(
        service.Id, service.VehiculoId, service.ClienteId, service.Patente, service.ClienteNombre,
        service.KilometrajeIngreso, service.TrabajoSolicitado, service.InspeccionPrevia,
        service.DanosExistentes, service.Observaciones, service.ManoObra, service.TotalProductos,
        service.Total, service.Estado, service.CreadoEnUtc, service.ActualizadoEnUtc,
        service.ActualizadoPorUsuarioId, service.FinalizadoEnUtc, service.AnuladoEnUtc,
        service.MotivoAnulacion,
        service.Detalles.Select(detail => new DetalleServicioDto(
            detail.Id, detail.ProductoId, detail.CodigoProducto, detail.NombreProducto,
            detail.UnidadMedida, detail.EsFraccionado, detail.Cantidad, detail.PrecioUnitario,
            detail.CostoUnitario, detail.Subtotal)).ToArray());
}
