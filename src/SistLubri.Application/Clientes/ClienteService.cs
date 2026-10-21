using SistLubri.Application.Common.Exceptions;
using SistLubri.Application.Common.Persistence;
using SistLubri.Application.Vehiculos;
using SistLubri.Domain.Clientes;

namespace SistLubri.Application.Clientes;

public sealed class ClienteService(
    IClienteRepository clienteRepository,
    IVehiculoRepository vehiculoRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<ClienteDetalleDto> CreateAsync(
        CrearClienteCommand command,
        CancellationToken cancellationToken = default)
    {
        var cliente = new Cliente(
            command.Nombre,
            command.Apellido,
            command.Telefono,
            command.Email,
            command.Domicilio,
            command.Observaciones);

        if (await clienteRepository.ExistsDuplicateAsync(
                cliente.Nombre,
                cliente.Apellido,
                cliente.Telefono,
                cancellationToken: cancellationToken))
        {
            throw new ConflictException("Ya existe un cliente con el mismo nombre, apellido y telefono.");
        }

        clienteRepository.Add(cliente);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return MapDetalle(cliente, []);
    }

    public async Task<ClienteDetalleDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var cliente = await GetClienteAsync(id, cancellationToken);
        var vehiculos = await vehiculoRepository.GetByClienteIdAsync(id, cancellationToken);
        return MapDetalle(cliente, vehiculos.Select(MapVehiculoResumen).ToArray());
    }

    public async Task<IReadOnlyList<ClienteResumenDto>> SearchAsync(
        string? search,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var clientes = await clienteRepository.SearchAsync(search, includeInactive, cancellationToken);
        return clientes.Select(MapResumen).ToArray();
    }

    public async Task<ClienteDetalleDto> UpdateAsync(
        Guid id,
        ActualizarClienteCommand command,
        CancellationToken cancellationToken = default)
    {
        var cliente = await GetClienteAsync(id, cancellationToken);
        if (!cliente.Activo)
        {
            throw new ConflictException("No se puede modificar un cliente inactivo.");
        }

        cliente.ActualizarDatos(
            command.Nombre,
            command.Apellido,
            command.Telefono,
            command.Email,
            command.Domicilio,
            command.Observaciones);

        if (await clienteRepository.ExistsDuplicateAsync(
                cliente.Nombre,
                cliente.Apellido,
                cliente.Telefono,
                id,
                cancellationToken))
        {
            throw new ConflictException("La modificacion produciria un cliente duplicado.");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ClienteDetalleDto> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var cliente = await GetClienteAsync(id, cancellationToken);
        if (!cliente.Activo)
        {
            throw new ConflictException("El cliente ya se encuentra inactivo.");
        }

        cliente.Desactivar();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ClienteDetalleDto> ReactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var cliente = await GetClienteAsync(id, cancellationToken);
        if (cliente.Activo)
        {
            throw new ConflictException("El cliente ya se encuentra activo.");
        }

        cliente.Reactivar();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task<Cliente> GetClienteAsync(Guid id, CancellationToken cancellationToken)
    {
        return await clienteRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("No se encontro el cliente solicitado.");
    }

    private static ClienteResumenDto MapResumen(Cliente cliente)
    {
        return new ClienteResumenDto(
            cliente.Id,
            cliente.Nombre,
            cliente.Apellido,
            cliente.Telefono,
            cliente.Email,
            cliente.Activo);
    }

    private static ClienteDetalleDto MapDetalle(
        Cliente cliente,
        IReadOnlyList<VehiculoResumenDto> vehiculos)
    {
        return new ClienteDetalleDto(
            cliente.Id,
            cliente.Nombre,
            cliente.Apellido,
            cliente.Telefono,
            cliente.Email,
            cliente.Domicilio,
            cliente.Observaciones,
            cliente.Activo,
            cliente.CreadoEnUtc,
            cliente.ActualizadoEnUtc,
            cliente.DesactivadoEnUtc,
            vehiculos);
    }

    private static VehiculoResumenDto MapVehiculoResumen(Domain.Vehiculos.Vehiculo vehiculo)
    {
        return new VehiculoResumenDto(
            vehiculo.Id,
            vehiculo.Patente,
            vehiculo.Marca,
            vehiculo.Modelo,
            vehiculo.Anio,
            vehiculo.KilometrajeActual,
            vehiculo.ProximoCambioKilometraje);
    }
}
