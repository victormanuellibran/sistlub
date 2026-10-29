using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using SistLubri.Application.Common.Persistence;
using SistLubri.Domain.Clientes;
using SistLubri.Domain.Caja;
using SistLubri.Domain.Productos;
using SistLubri.Domain.Servicios;
using SistLubri.Domain.Vehiculos;
using SistLubri.Domain.Ventas;
using SistLubri.Domain.Proveedores;
using SistLubri.Domain.Gastos;
using SistLubri.Infrastructure.Persistence.Usuarios;

namespace SistLubri.Infrastructure.Persistence;

public sealed class SistLubriDbContext(DbContextOptions<SistLubriDbContext> options)
    : IdentityDbContext<UsuarioSistema, IdentityRole<Guid>, Guid>(options), IUnitOfWork
{
    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        new UnitOfWorkTransaction(await Database.BeginTransactionAsync(cancellationToken));

    private sealed class UnitOfWorkTransaction(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction) : IUnitOfWorkTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();

    public DbSet<Producto> Productos => Set<Producto>();

    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();

    public DbSet<ServicioVehicular> ServiciosVehiculares => Set<ServicioVehicular>();

    public DbSet<DetalleServicio> DetallesServicio => Set<DetalleServicio>();

    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();
    public DbSet<PagoVenta> PagosVenta => Set<PagoVenta>();
    public DbSet<ReintegroVenta> ReintegrosVenta => Set<ReintegroVenta>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<RegistroEstadoProveedor> RegistrosEstadoProveedor => Set<RegistroEstadoProveedor>();
    public DbSet<Gasto> Gastos => Set<Gasto>();
    public DbSet<PagoGasto> PagosGasto => Set<PagoGasto>();
    public DbSet<ReintegroGasto> ReintegrosGasto => Set<ReintegroGasto>();
    public DbSet<JornadaCaja> JornadasCaja => Set<JornadaCaja>();
    public DbSet<MovimientoCaja> MovimientosCaja => Set<MovimientoCaja>();
    public DbSet<MedioPago> MediosPago => Set<MedioPago>();
    public DbSet<ConfiguracionCaja> ConfiguracionesCaja => Set<ConfiguracionCaja>();
    public DbSet<RegistroCierreCaja> RegistrosCierreCaja => Set<RegistroCierreCaja>();
    public DbSet<RegistroAperturaCaja> RegistrosAperturaCaja => Set<RegistroAperturaCaja>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SistLubriDbContext).Assembly);
    }
}
