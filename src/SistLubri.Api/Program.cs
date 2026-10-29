using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using SistLubri.Api.Auth;
using SistLubri.Api.Caja;
using SistLubri.Api.Common;
using SistLubri.Application.Clientes;
using SistLubri.Application.Caja;
using SistLubri.Application.Productos;
using SistLubri.Application.Servicios;
using SistLubri.Application.Vehiculos;
using SistLubri.Application.Ventas;
using SistLubri.Application.Proveedores;
using SistLubri.Application.Common.Security;
using SistLubri.Infrastructure;
using SistLubri.Infrastructure.Persistence;
using SistLubri.Infrastructure.Persistence.Usuarios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<VehiculoService>();
builder.Services.AddScoped<ProductoService>();
builder.Services.AddScoped<ServicioVehicularService>();
builder.Services.AddScoped<VentaService>();
builder.Services.AddScoped<CajaService>();
builder.Services.AddScoped<ProveedorService>();
builder.Services.AddHostedService<CajaScheduler>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No se configuro la conexion DefaultConnection.");

builder.Services.AddInfrastructure(connectionString);

var configuredSigningKey = builder.Configuration["Auth:SigningKey"];
var signingKey = string.IsNullOrWhiteSpace(configuredSigningKey)
    ? appKeyForEnvironment(builder.Environment)
    : Convert.FromBase64String(configuredSigningKey);
if (signingKey.Length < 32)
{
    throw new InvalidOperationException("Auth:SigningKey debe contener al menos 256 bits (32 bytes) en Base64.");
}

var jwtSettings = new JwtSettings(
    builder.Configuration["Auth:Issuer"] ?? "SistLubri",
    builder.Configuration["Auth:Audience"] ?? "SistLubri.Api",
    signingKey,
    TimeSpan.FromMinutes(20));
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddScoped<JwtTokenIssuer>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtSettings.SigningKey),
            ValidateLifetime = true,
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userId, out var id))
                {
                    context.Fail("Token de usuario invalido.");
                    return;
                }

                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<UsuarioSistema>>();
                var user = await userManager.FindByIdAsync(id.ToString());
                if (user is null || !user.Activo)
                {
                    context.Fail("La cuenta no existe o esta desactivada.");
                }
            }
        };
        options.Events.OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";
            return context.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Autenticacion requerida",
                Detail = "Inicie sesion y envie el token Bearer en la solicitud."
            });
        };
        options.Events.OnForbidden = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/problem+json";
            return context.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Permiso insuficiente",
                Detail = "Su usuario no tiene permisos para esta operacion."
            });
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "SistLubri API v1");
    });

    if (builder.Configuration.GetValue("Database:ApplyMigrationsOnStart", true))
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SistLubriDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static byte[] appKeyForEnvironment(IHostEnvironment environment)
{
    if (!environment.IsDevelopment())
    {
        throw new InvalidOperationException("Configure Auth:SigningKey como secreto Base64 fuera de Development.");
    }

    return RandomNumberGenerator.GetBytes(64);
}
