using SistLubri.Application.Common.Exceptions;
using SistLubri.Application.Common.Persistence;
using SistLubri.Application.Common.Security;
using SistLubri.Domain.Proveedores;

namespace SistLubri.Application.Proveedores;

public sealed class ProveedorService(
    IProveedorRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
{
    public async Task<ProveedorDetalleDto> CreateAsync(
        CrearProveedorCommand command,
        CancellationToken cancellationToken = default)
    {
        var cuit = Proveedor.NormalizarCuit(command.Cuit);
        if (cuit is not null && await repository.ExistsCuitAsync(cuit, cancellationToken: cancellationToken))
            throw new ConflictException("Ya existe un proveedor registrado con ese CUIT.");

        var proveedor = new Proveedor(command.RazonSocial, cuit, command.ContactoNombre, command.Telefono,
            command.Email, command.Direccion, command.MarcasYProductos, command.CondicionPagoHabitual,
            command.PlazoPagoHabitualDias, command.PeriodoPagoHabitual, command.Observaciones, currentUser.Id);
        await EnsurePossibleDuplicatesConfirmedAsync(proveedor, command.ConfirmarPosiblesDuplicados, null, cancellationToken);

        repository.Add(proveedor);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(proveedor);
    }

    public async Task<IReadOnlyList<ProveedorResumenDto>> SearchAsync(
        string? search,
        bool includeInactive,
        CancellationToken cancellationToken = default) =>
        (await repository.SearchAsync(search, includeInactive, cancellationToken)).Select(MapResumen).ToArray();

    public async Task<ProveedorDetalleDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Map(await GetProveedorAsync(id, cancellationToken));

    public async Task<ProveedorDetalleDto> UpdateAsync(
        Guid id,
        ActualizarProveedorCommand command,
        CancellationToken cancellationToken = default)
    {
        var proveedor = await GetProveedorAsync(id, cancellationToken);
        var cuit = Proveedor.NormalizarCuit(command.Cuit);
        if (cuit is not null && await repository.ExistsCuitAsync(cuit, id, cancellationToken))
            throw new ConflictException("El CUIT ya pertenece a otro proveedor.");

        var candidate = new Proveedor(command.RazonSocial, cuit, command.ContactoNombre, command.Telefono,
            command.Email, command.Direccion, command.MarcasYProductos, command.CondicionPagoHabitual,
            command.PlazoPagoHabitualDias, command.PeriodoPagoHabitual, command.Observaciones, currentUser.Id);
        await EnsurePossibleDuplicatesConfirmedAsync(candidate, command.ConfirmarPosiblesDuplicados, id, cancellationToken);

        proveedor.ActualizarDatos(command.RazonSocial, cuit, command.ContactoNombre, command.Telefono,
            command.Email, command.Direccion, command.MarcasYProductos, command.CondicionPagoHabitual,
            command.PlazoPagoHabitualDias, command.PeriodoPagoHabitual, command.Observaciones, currentUser.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(proveedor);
    }

    public async Task<ProveedorDetalleDto> DeactivateAsync(
        Guid id,
        DesactivarProveedorCommand command,
        CancellationToken cancellationToken = default)
    {
        var proveedor = await GetProveedorAsync(id, cancellationToken);
        if (!proveedor.Activo)
            throw new ConflictException("El proveedor ya se encuentra inactivo.");
        proveedor.Desactivar(command.Motivo, currentUser.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(proveedor);
    }

    public async Task<ProveedorDetalleDto> ReactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var proveedor = await GetProveedorAsync(id, cancellationToken);
        if (proveedor.Activo)
            throw new ConflictException("El proveedor ya se encuentra activo.");
        proveedor.Reactivar(currentUser.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(proveedor);
    }

    private async Task EnsurePossibleDuplicatesConfirmedAsync(
        Proveedor candidate,
        bool confirmed,
        Guid? excludingId,
        CancellationToken cancellationToken)
    {
        var possible = await repository.GetPotentialDuplicatesAsync(candidate.RazonSocial, candidate.Telefono,
            candidate.Email, excludingId, cancellationToken);
        if (possible.Count == 0 || confirmed) return;

        var matches = string.Join("; ", possible.Take(5).Select(item =>
            $"{item.RazonSocial} (CUIT: {item.Cuit ?? "sin CUIT"}, ID: {item.Id})"));
        throw new ConflictException(
            $"Se encontraron posibles proveedores duplicados: {matches}. Revise esos registros y reintente con ConfirmarPosiblesDuplicados=true si se trata de otra entidad.");
    }

    private async Task<Proveedor> GetProveedorAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("No se encontro el proveedor solicitado.");

    private static ProveedorResumenDto MapResumen(Proveedor proveedor) =>
        new(proveedor.Id, proveedor.RazonSocial, proveedor.Cuit, proveedor.Telefono, proveedor.Activo);

    private static ProveedorDetalleDto Map(Proveedor proveedor) => new(
        proveedor.Id, proveedor.RazonSocial, proveedor.Cuit, proveedor.ContactoNombre, proveedor.Telefono,
        proveedor.Email, proveedor.Direccion, proveedor.MarcasYProductos, proveedor.CondicionPagoHabitual,
        proveedor.PlazoPagoHabitualDias, proveedor.PeriodoPagoHabitual, proveedor.Observaciones,
        proveedor.Activo, proveedor.CreadoEnUtc, proveedor.CreadoPorUsuarioId,
        proveedor.ActualizadoEnUtc, proveedor.ActualizadoPorUsuarioId,
        proveedor.HistorialEstados.OrderBy(entry => entry.FechaUtc)
            .Select(entry => new HistorialEstadoProveedorDto(entry.Estado.ToString(), entry.Motivo,
                entry.FechaUtc, entry.UsuarioId)).ToArray());
}
