using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistLubri.Application.Clientes;
using SistLubri.Application.Common.Persistence;
using SistLubri.Application.Vehiculos;
using SistLubri.Infrastructure.Persistence;
using SistLubri.Infrastructure.Persistence.Repositories;

namespace SistLubri.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<SistLubriDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IVehiculoRepository, VehiculoRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<SistLubriDbContext>());

        return services;
    }
}
