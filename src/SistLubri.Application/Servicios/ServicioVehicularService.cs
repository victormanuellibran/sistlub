using SistLubri.Application.Common.Exceptions;
using SistLubri.Application.Common.Persistence;
using SistLubri.Application.Clientes;
using SistLubri.Application.Productos;
using SistLubri.Application.Vehiculos;
using SistLubri.Domain.Productos;
using SistLubri.Domain.Servicios;

namespace SistLubri.Application.Servicios;

public sealed class ServicioVehicularService(
    IServicioVehicularRepository servicioRepository,
    IVehiculoRepository vehiculoRepository,
    IClienteRepository clienteRepository,
    IProductoRepository productoRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<ServicioVehicularDto> CreateAsync(
        CrearServicioVehicularCommand command,
        CancellationToken cancellationToken = default)
    {
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
            throw new ConflictException(
                "El kilometraje de ingreso no puede ser menor al ultimo registrado para esta patente.");
        }

        var servicio = new ServicioVehicular(
            vehiculo.Id,
            vehiculo.ClienteId,
            vehiculo.Patente,
            $"{cliente.Nombre} {cliente.Apellido}",
            command.KilometrajeIngreso,
            command.TrabajoSolicitado,
            command.InspeccionPrevia,
            command.DanosExistentes,
            command.Observaciones,
            command.ManoObra);

        await AddDetailsAsync(servicio, command.Detalles, false, cancellationToken);

        servicioRepository.Add(servicio);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(servicio);
    }

    public async Task<ServicioVehicularDto> UpdateDetailsAsync(
        Guid id,
        IReadOnlyList<CrearDetalleServicioCommand> details,
        CancellationToken cancellationToken = default)
    {
        var servicio = await GetServicioAsync(id, cancellationToken);
        await AddDetailsAsync(servicio, details, true, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(servicio);
    }

    public async Task<ServicioVehicularDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var servicio = await GetServicioAsync(id, cancellationToken);
        return Map(servicio);
    }

    public async Task<IReadOnlyList<ServicioVehicularDto>> GetHistoryByPlateAsync(
        string patente,
        CancellationToken cancellationToken = default)
    {
        var normalizedPlate = SistLubri.Domain.Vehiculos.Vehiculo.NormalizarPatente(patente);
        var vehiculo = await vehiculoRepository.GetByPatenteAsync(normalizedPlate, cancellationToken)
            ?? throw new NotFoundException("No se encontro un vehiculo con esa patente.");
        var history = await servicioRepository.GetByVehiculoIdAsync(vehiculo.Id, cancellationToken);
        return history.Select(Map).ToArray();
    }

    public async Task<ServicioVehicularDto> FinalizeAsync(
        Guid id,
        int kilometrajeFinal,
        int? proximoCambioKilometraje,
        CancellationToken cancellationToken = default)
    {
        var servicio = await GetServicioAsync(id, cancellationToken);
        var vehiculo = await vehiculoRepository.GetByIdAsync(servicio.VehiculoId, cancellationToken)
            ?? throw new NotFoundException("No se encontro el vehiculo asociado al servicio.");

        if (vehiculo.Patente != servicio.Patente)
        {
            throw new ConflictException("La patente del vehiculo cambio desde que se inicio el servicio.");
        }

        if (kilometrajeFinal < servicio.KilometrajeIngreso)
        {
            throw new ConflictException("El kilometraje final no puede ser menor al kilometraje de ingreso.");
        }

        if (vehiculo.KilometrajeActual.HasValue && kilometrajeFinal < vehiculo.KilometrajeActual)
        {
            throw new ConflictException(
                "El kilometraje final es menor al ultimo registrado para esta patente.");
        }

        if (proximoCambioKilometraje.HasValue && proximoCambioKilometraje <= kilometrajeFinal)
        {
            throw new ConflictException(
                "El kilometraje del proximo cambio debe ser mayor al kilometraje final.");
        }

        var nextServiceMileage = proximoCambioKilometraje ?? vehiculo.ProximoCambioKilometraje;
        foreach (var detalle in servicio.Detalles)
        {
            var producto = await productoRepository.GetByIdAsync(detalle.ProductoId, cancellationToken)
                ?? throw new NotFoundException($"No se encontro el producto '{detalle.NombreProducto}'.");

            if (!producto.Activo)
            {
                throw new ConflictException($"El producto '{producto.Nombre}' esta inactivo.");
            }

            if (producto.UnidadMedida != detalle.UnidadMedida || producto.EsFraccionado != detalle.EsFraccionado)
            {
                throw new ConflictException(
                    $"La unidad o modalidad del producto '{producto.Nombre}' cambio desde que se agrego al servicio.");
            }

            var movimiento = producto.RegistrarMovimiento(
                TipoMovimientoInventario.Salida,
                detalle.Cantidad,
                $"Consumo del servicio {servicio.Id}",
                servicio.Id);
            productoRepository.AddMovement(movimiento);
        }

        vehiculo.ActualizarDatos(
            vehiculo.Marca,
            vehiculo.Modelo,
            vehiculo.Anio,
            kilometrajeFinal,
            nextServiceMileage,
            vehiculo.Observaciones);
        servicio.Finalizar();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(servicio);
    }

    public async Task<ServicioVehicularDto> CancelAsync(
        Guid id,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        var servicio = await GetServicioAsync(id, cancellationToken);
        servicio.Anular(motivo);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(servicio);
    }

    private async Task<ServicioVehicular> GetServicioAsync(Guid id, CancellationToken cancellationToken)
    {
        return await servicioRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("No se encontro el servicio solicitado.");
    }

    private async Task AddDetailsAsync(
        ServicioVehicular service,
        IReadOnlyList<CrearDetalleServicioCommand> details,
        bool replace,
        CancellationToken cancellationToken)
    {
        if (details.Select(detail => detail.ProductoId).Distinct().Count() != details.Count)
        {
            throw new SistLubri.Domain.Common.DomainException(
                "Cada producto debe aparecer una sola vez; consolide las cantidades.");
        }

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

        if (replace)
        {
            service.EliminarDetallesExcepto(products.Select(item => item.Product.Id).ToHashSet());
        }

        foreach (var (product, detail) in products)
        {
            service.AgregarOActualizarDetalle(
                product.Id,
                product.Codigo,
                product.Nombre,
                product.UnidadMedida,
                product.EsFraccionado,
                detail.Cantidad,
                product.PrecioVenta,
                product.CostoCompra);
        }
    }

    private static ServicioVehicularDto Map(ServicioVehicular servicio)
    {
        return new ServicioVehicularDto(
            servicio.Id,
            servicio.VehiculoId,
            servicio.ClienteId,
            servicio.Patente,
            servicio.ClienteNombre,
            servicio.KilometrajeIngreso,
            servicio.TrabajoSolicitado,
            servicio.InspeccionPrevia,
            servicio.DanosExistentes,
            servicio.Observaciones,
            servicio.ManoObra,
            servicio.TotalProductos,
            servicio.Total,
            servicio.Estado,
            servicio.CreadoEnUtc,
            servicio.FinalizadoEnUtc,
            servicio.AnuladoEnUtc,
            servicio.MotivoAnulacion,
            servicio.Detalles.Select(detalle => new DetalleServicioDto(
                detalle.Id,
                detalle.ProductoId,
                detalle.CodigoProducto,
                detalle.NombreProducto,
                detalle.UnidadMedida,
                detalle.EsFraccionado,
                detalle.Cantidad,
                detalle.PrecioUnitario,
                detalle.CostoUnitario,
                detalle.Subtotal)).ToArray());
    }
}
