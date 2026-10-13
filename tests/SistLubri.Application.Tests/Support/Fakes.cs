using SistLubri.Application.Clientes;
using SistLubri.Application.Common.Persistence;
using SistLubri.Application.Productos;
using SistLubri.Application.Vehiculos;
using SistLubri.Domain.Clientes;
using SistLubri.Domain.Productos;
using SistLubri.Domain.Vehiculos;

namespace SistLubri.Application.Tests.Support;

internal sealed class FakeClienteRepository(params Cliente[] clientes) : IClienteRepository
{
    public List<Cliente> Items { get; } = [.. clientes];

    public Task<Cliente?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Items.SingleOrDefault(cliente => cliente.Id == id));
    }

    public Task<IReadOnlyList<Cliente>> SearchAsync(
        string? search,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Cliente> result = Items
            .Where(cliente => includeInactive || cliente.Activo)
            .ToArray();
        return Task.FromResult(result);
    }

    public Task<bool> ExistsDuplicateAsync(
        string nombre,
        string apellido,
        string telefono,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        var exists = Items.Any(cliente =>
            string.Equals(cliente.Nombre, nombre, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(cliente.Apellido, apellido, StringComparison.OrdinalIgnoreCase) &&
            cliente.Telefono == telefono &&
            (!excludingId.HasValue || cliente.Id != excludingId.Value));

        return Task.FromResult(exists);
    }

    public void Add(Cliente cliente)
    {
        Items.Add(cliente);
    }
}

internal sealed class FakeVehiculoRepository(params Vehiculo[] vehiculos) : IVehiculoRepository
{
    public List<Vehiculo> Items { get; } = [.. vehiculos];

    public Task<Vehiculo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Items.SingleOrDefault(vehiculo => vehiculo.Id == id));
    }

    public Task<Vehiculo?> GetByPatenteAsync(string patente, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Items.SingleOrDefault(vehiculo => vehiculo.Patente == patente));
    }

    public Task<IReadOnlyList<Vehiculo>> SearchAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Vehiculo>>(Items.ToArray());
    }

    public Task<IReadOnlyList<Vehiculo>> GetByClienteIdAsync(
        Guid clienteId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Vehiculo> result = Items
            .Where(vehiculo => vehiculo.ClienteId == clienteId)
            .ToArray();
        return Task.FromResult(result);
    }

    public Task<bool> ExistsPatenteAsync(
        string patente,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        var exists = Items.Any(vehiculo =>
            vehiculo.Patente == patente &&
            (!excludingId.HasValue || vehiculo.Id != excludingId.Value));
        return Task.FromResult(exists);
    }

    public void Add(Vehiculo vehiculo)
    {
        Items.Add(vehiculo);
    }
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }
}

internal sealed class FakeProductoRepository(params Producto[] productos) : IProductoRepository
{
    public List<Producto> Items { get; } = [.. productos];

    public Task<Producto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Items.SingleOrDefault(producto => producto.Id == id));
    }

    public Task<IReadOnlyList<Producto>> SearchAsync(
        string? search,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Producto> result = Items
            .Where(producto => includeInactive || producto.Activo)
            .ToArray();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<Producto>> GetAlertsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Producto> result = Items
            .Where(producto =>
                producto.Activo &&
                producto.EstadoInventario != EstadoInventario.Disponible)
            .ToArray();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<MovimientoInventario>> GetMovementsAsync(
        Guid productoId,
        TipoMovimientoInventario? tipo,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<MovimientoInventario> result = Items
            .Single(producto => producto.Id == productoId)
            .Movimientos
            .Where(movimiento => !tipo.HasValue || movimiento.Tipo == tipo.Value)
            .ToArray();
        return Task.FromResult(result);
    }

    public Task<bool> ExistsCodeAsync(
        string codigo,
        Guid? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        var exists = Items.Any(producto =>
            producto.Codigo == codigo &&
            (!excludingId.HasValue || producto.Id != excludingId.Value));
        return Task.FromResult(exists);
    }

    public void Add(Producto producto)
    {
        Items.Add(producto);
    }

    public void AddMovement(MovimientoInventario movimiento)
    {
    }
}
