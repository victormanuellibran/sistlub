using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistLubri.Application.Clientes;
using SistLubri.Application.Caja;
using SistLubri.Application.Common.Persistence;
using SistLubri.Application.Productos;
using SistLubri.Application.Servicios;
using SistLubri.Application.Vehiculos;
using SistLubri.Application.Ventas;
using SistLubri.Application.Proveedores;
using SistLubri.Application.Gastos;
using SistLubri.Infrastructure.Persistence;
using SistLubri.Infrastructure.Persistence.Repositories;
using SistLubri.Infrastructure.Persistence.Usuarios;

namespace SistLubri.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<SistLubriDbContext>(options => options.UseSqlServer(connectionString));
        services.AddIdentityCore<UsuarioSistema>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<SistLubriDbContext>()
            .AddSignInManager();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IVehiculoRepository, VehiculoRepository>();
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddScoped<IServicioVehicularRepository, ServicioVehicularRepository>();
        services.AddScoped<IVentaRepository, VentaRepository>();
        services.AddScoped<ICajaRepository, CajaRepository>();
        services.AddScoped<IProveedorRepository, ProveedorRepository>();
        services.AddScoped<IGastoRepository, GastoRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<SistLubriDbContext>());



        return services;
    }
}
