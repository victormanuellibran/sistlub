using SistLubri.Application.Clientes;
using SistLubri.Application.Common.Exceptions;
using SistLubri.Application.Common.Persistence;
using SistLubri.Domain.Clientes;
using SistLubri.Domain.Vehiculos;

namespace SistLubri.Application.Vehiculos;

public sealed class VehiculoService(
    IVehiculoRepository vehiculoRepository,
    IClienteRepository clienteRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<VehiculoDetalleDto> CreateAsync(
        Guid clienteId,
        CrearVehiculoCommand command,
        CancellationToken cancellationToken = default)
    {
        var cliente = await GetClienteActivoAsync(clienteId, cancellationToken);
        var vehiculo = new Vehiculo(
            clienteId,
            command.Patente,
            command.Marca,
            command.Modelo,
            command.Anio,
            command.KilometrajeActual,
            command.ProximoCambioKilometraje,
            command.Observaciones);

        if (await vehiculoRepository.ExistsPatenteAsync(vehiculo.Patente, cancellationToken: cancellationToken))
        {
            throw new ConflictException("Ya existe un vehiculo registrado con esa patente.");
        }

        vehiculoRepository.Add(vehiculo);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetalle(vehiculo, cliente);
    }

    public async Task<VehiculoDetalleDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var vehiculo = await vehiculoRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("No se encontro el vehiculo solicitado.");

        return MapDetalle(vehiculo, vehiculo.Cliente);
    }

    public async Task<VehiculoDetalleDto> GetByPatenteAsync(
        string patente,
        CancellationToken cancellationToken = default)
    {
        var patenteNormalizada = Vehiculo.NormalizarPatente(patente);
        var vehiculo = await vehiculoRepository.GetByPatenteAsync(patenteNormalizada, cancellationToken)
            ?? throw new NotFoundException("No se encontro un vehiculo con esa patente.");

        return MapDetalle(vehiculo, vehiculo.Cliente);
    }

    public async Task<IReadOnlyList<VehiculoDetalleDto>> SearchAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var vehiculos = await vehiculoRepository.SearchAsync(search, cancellationToken);
        return vehiculos.Select(v => MapDetalle(v, v.Cliente)).ToArray();
    }

    public async Task<VehiculoDetalleDto> UpdateAsync(
        Guid id,
        ActualizarVehiculoCommand command,
        CancellationToken cancellationToken = default)
    {
        var vehiculo = await vehiculoRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("No se encontro el vehiculo solicitado.");

        vehiculo.ActualizarDatos(
            command.Marca,
            command.Modelo,
            command.Anio,
            command.KilometrajeActual,
            command.ProximoCambioKilometraje,
            command.Observaciones);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetalle(vehiculo, vehiculo.Cliente);
    }

    public async Task<VehiculoDetalleDto> ChangeOwnerAsync(
        Guid id,
        Guid clienteId,
        CancellationToken cancellationToken = default)
    {
        var vehiculo = await vehiculoRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("No se encontro el vehiculo solicitado.");
        var cliente = await GetClienteActivoAsync(clienteId, cancellationToken);

        vehiculo.CambiarTitular(clienteId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetalle(vehiculo, cliente);
    }

    private async Task<Cliente> GetClienteActivoAsync(Guid id, CancellationToken cancellationToken)
    {
        var cliente = await clienteRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("No se encontro el cliente solicitado.");

        if (!cliente.Activo)
        {
            throw new ConflictException("No se puede asociar un vehiculo a un cliente inactivo.");
        }

        return cliente;
    }

    private static VehiculoDetalleDto MapDetalle(Vehiculo vehiculo, Cliente cliente)
    {
        return new VehiculoDetalleDto(
            vehiculo.Id,
            vehiculo.ClienteId,
            $"{cliente.Nombre} {cliente.Apellido}",
            vehiculo.Patente,
            vehiculo.Marca,
            vehiculo.Modelo,
            vehiculo.Anio,
            vehiculo.KilometrajeActual,
            vehiculo.ProximoCambioKilometraje,
            vehiculo.Observaciones,
            vehiculo.CreadoEnUtc,
            vehiculo.ActualizadoEnUtc);
    }
}
