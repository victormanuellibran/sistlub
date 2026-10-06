using Microsoft.EntityFrameworkCore;
using SistLubri.Application.Common.Persistence;
using SistLubri.Domain.Clientes;
using SistLubri.Domain.Vehiculos;

namespace SistLubri.Infrastructure.Persistence;

public sealed class SistLubriDbContext(DbContextOptions<SistLubriDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SistLubriDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
