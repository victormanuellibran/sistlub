using Microsoft.EntityFrameworkCore;
using SistLubri.Api.Common;
using SistLubri.Application.Clientes;
using SistLubri.Application.Vehiculos;
using SistLubri.Infrastructure;
using SistLubri.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<VehiculoService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No se configuro la conexion DefaultConnection.");

builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "SistLubri API v1");
    });

    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<SistLubriDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseAuthorization();

app.MapControllers();

app.Run();
