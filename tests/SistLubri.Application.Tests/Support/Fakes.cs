using SistLubri.Application.Clientes;
using SistLubri.Application.Common.Persistence;
using SistLubri.Application.Productos;
using SistLubri.Application.Servicios;
using SistLubri.Application.Vehiculos;
using SistLubri.Application.Ventas;
using SistLubri.Domain.Clientes;
using SistLubri.Domain.Productos;
using SistLubri.Domain.Servicios;
using SistLubri.Domain.Vehiculos;
using SistLubri.Domain.Ventas;
using SistLubri.Domain.Proveedores;

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

    public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IUnitOfWorkTransaction>(new FakeUnitOfWorkTransaction());

    private sealed class FakeUnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
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

internal sealed class FakeServicioVehicularRepository(params ServicioVehicular[] servicios)
    : IServicioVehicularRepository
{
    public List<ServicioVehicular> Items { get; } = [.. servicios];

    public Task<ServicioVehicular?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Items.SingleOrDefault(servicio => servicio.Id == id));
    }

    public Task<IReadOnlyList<ServicioVehicular>> GetByVehiculoIdAsync(
        Guid vehiculoId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ServicioVehicular> result = Items
            .Where(servicio => servicio.VehiculoId == vehiculoId)
            .OrderByDescending(servicio => servicio.CreadoEnUtc)
            .ToArray();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<ServicioVehicular>> GetInProgressAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ServicioVehicular> result = Items
            .Where(service => service.Estado == EstadoServicio.EnCurso)
            .ToArray();
        return Task.FromResult(result);
    }

    public void Add(ServicioVehicular servicio)
    {
        Items.Add(servicio);
    }
}

internal sealed class FakeVentaRepository : IVentaRepository
{
    public List<Venta> Items { get; } = [];

    public Task<Venta?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(sale => sale.Id == id));

    public Task<Venta?> GetByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(sale => sale.ServicioId == serviceId));

    public Task<bool> ExistsByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(sale => sale.ServicioId == serviceId));

    public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(sale => sale.Id == id));

    public Task<IReadOnlyList<Venta>> SearchAsync(
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        Guid? clienteId,
        EstadoVenta? estado,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Venta> result = Items.ToArray();
        return Task.FromResult(result);
    }

    public void Add(Venta venta) => Items.Add(venta);
}

internal sealed class FakeCurrentUser : SistLubri.Application.Common.Security.ICurrentUser
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Nombre { get; } = "Usuario de pruebas";
}

internal sealed class FakeCajaRepository(
    SistLubri.Domain.Caja.JornadaCaja? jornada = null,
    params SistLubri.Domain.Caja.MedioPago[] mediosPago) : SistLubri.Application.Caja.ICajaRepository
{
    public List<SistLubri.Domain.Caja.JornadaCaja> Jornadas { get; } = jornada is null ? [] : [jornada];
    public List<SistLubri.Domain.Caja.MovimientoCaja> AddedMovements { get; } = [];
    public SistLubri.Domain.Caja.ConfiguracionCaja? Configuracion { get; set; }
    public Task<SistLubri.Domain.Caja.JornadaCaja?> GetActualAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Jornadas.SingleOrDefault(day => day.Estado is SistLubri.Domain.Caja.EstadoJornadaCaja.Abierta or SistLubri.Domain.Caja.EstadoJornadaCaja.Reabierta));
    public Task<SistLubri.Domain.Caja.JornadaCaja?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Jornadas.SingleOrDefault(day => day.Id == id));
    public Task<SistLubri.Domain.Caja.JornadaCaja?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default) =>
        Task.FromResult(Jornadas.SingleOrDefault(day => day.Fecha == date));
    public Task<SistLubri.Domain.Caja.JornadaCaja?> GetMostRecentBeforeAsync(DateOnly date, CancellationToken cancellationToken = default) =>
        Task.FromResult(Jornadas.Where(day => day.Fecha < date).OrderByDescending(day => day.Fecha).FirstOrDefault());
    public Task<SistLubri.Domain.Caja.ConfiguracionCaja?> GetConfigurationAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Configuracion);
    public Task<bool> HasOpenAsync(CancellationToken cancellationToken = default) => Task.FromResult(Jornadas.Any(day => day.Estado is SistLubri.Domain.Caja.EstadoJornadaCaja.Abierta or SistLubri.Domain.Caja.EstadoJornadaCaja.Reabierta));
    public Task<bool> ExistsOnDateAsync(DateOnly date, CancellationToken cancellationToken = default) => Task.FromResult(Jornadas.Any(day => day.Fecha == date));
    public Task<bool> ExistsAnyAsync(CancellationToken cancellationToken = default) => Task.FromResult(Jornadas.Count > 0);
    public Task<IReadOnlyList<SistLubri.Domain.Caja.JornadaCaja>> SearchAsync(DateOnly? desde, DateOnly? hasta, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SistLubri.Domain.Caja.JornadaCaja>>(Jornadas.ToArray());
    public Task<IReadOnlyList<SistLubri.Domain.Caja.MedioPago>> GetMediosPagoAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SistLubri.Domain.Caja.MedioPago>>(mediosPago);
    public Task<SistLubri.Domain.Caja.MedioPago?> GetMedioPagoAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(mediosPago.SingleOrDefault(method => method.Id == id));
    public Task<bool> ExistsPaymentMethodNameAsync(string nombre, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
    public void Add(SistLubri.Domain.Caja.JornadaCaja item) => Jornadas.Add(item);
    public void Add(SistLubri.Domain.Caja.MovimientoCaja item) => AddedMovements.Add(item);
    public void Add(SistLubri.Domain.Caja.MedioPago item) { }
    public void Add(SistLubri.Domain.Caja.ConfiguracionCaja item) { }
    public void Add(SistLubri.Domain.Caja.RegistroCierreCaja item) { }
    public void Add(SistLubri.Domain.Caja.RegistroAperturaCaja item) { }
}

internal sealed class FakeProveedorRepository(params Proveedor[] proveedores) : SistLubri.Application.Proveedores.IProveedorRepository
{
    public List<Proveedor> Items { get; } = [.. proveedores];

    public Task<Proveedor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(item => item.Id == id));

    public Task<IReadOnlyList<Proveedor>> SearchAsync(string? search, bool includeInactive, CancellationToken cancellationToken = default)
    {
        IEnumerable<Proveedor> query = Items.Where(item => includeInactive || item.Activo);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(item => item.RazonSocial.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (item.Cuit?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                item.Telefono.Contains(term, StringComparison.OrdinalIgnoreCase));
        }
        return Task.FromResult<IReadOnlyList<Proveedor>>(query.OrderBy(item => item.RazonSocial).Take(100).ToArray());
    }

    public Task<bool> ExistsCuitAsync(string cuit, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(item => item.Cuit == cuit && (!excludingId.HasValue || item.Id != excludingId.Value)));

    public Task<IReadOnlyList<Proveedor>> GetPotentialDuplicatesAsync(string razonSocial, string? telefono, string? email,
        Guid? excludingId = null, CancellationToken cancellationToken = default)
    {
        var phoneDigits = telefono is null ? string.Empty : new string(telefono.Where(char.IsDigit).ToArray());
        var normalizedEmail = email?.Trim();
        IReadOnlyList<Proveedor> matches = Items.Where(item =>
            (!excludingId.HasValue || item.Id != excludingId.Value) &&
            (string.Equals(item.RazonSocial, razonSocial.Trim(), StringComparison.OrdinalIgnoreCase) ||
             (phoneDigits.Length > 0 && item.Telefono == phoneDigits) ||
             (normalizedEmail is not null && string.Equals(item.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))))
            .Take(20).ToArray();
        return Task.FromResult(matches);
    }

    public void Add(Proveedor proveedor) => Items.Add(proveedor);
}
