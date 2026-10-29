using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistLubri.Api.Auth;
using SistLubri.Api.Contracts.Auth;
using SistLubri.Infrastructure.Persistence;
using SistLubri.Infrastructure.Persistence.Usuarios;

namespace SistLubri.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController(
    UserManager<UsuarioSistema> userManager,
    SignInManager<UsuarioSistema> signInManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    SistLubriDbContext dbContext,
    IConfiguration configuration,
    JwtTokenIssuer tokenIssuer) : ControllerBase
{
    [HttpPost("bootstrap")]
    [ProducesResponseType<SesionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SesionResponse>> Bootstrap(
        InicializarAdministradorRequest request,
        CancellationToken cancellationToken)
    {
        var configuredSecret = configuration["Auth:BootstrapSecret"];
        if (string.IsNullOrWhiteSpace(configuredSecret))
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Inicializacion deshabilitada",
                detail: "Configure Auth:BootstrapSecret mediante una variable de entorno para crear el primer administrador.");
        }

        var providedSecret = Encoding.UTF8.GetBytes(request.ClaveInicial);
        var expectedSecret = Encoding.UTF8.GetBytes(configuredSecret);
        if (!CryptographicOperations.FixedTimeEquals(providedSecret, expectedSecret))
        {
            return Unauthorized();
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        if (await userManager.Users.AnyAsync(cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "El sistema ya fue inicializado."
            });
        }

        foreach (var roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName)
                {
                    Id = Guid.NewGuid()
                });
                if (!roleResult.Succeeded)
                {
                    return IdentityValidationProblem(roleResult);
                }
            }
        }

        var user = new UsuarioSistema
        {
            Id = Guid.NewGuid(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            EmailConfirmed = true,
            NombreCompleto = request.NombreCompleto.Trim(),
            Activo = true,
            CreadoEnUtc = DateTimeOffset.UtcNow
        };
        var createResult = await userManager.CreateAsync(user, request.Contrasena);
        if (!createResult.Succeeded)
        {
            return IdentityValidationProblem(createResult);
        }

        var roleAssignment = await userManager.AddToRoleAsync(user, Roles.Administrator);
        if (!roleAssignment.Succeeded)
        {
            return IdentityValidationProblem(roleAssignment);
        }

        await transaction.CommitAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, await CreateSessionAsync(user));
    }

    [HttpPost("login")]
    [ProducesResponseType<SesionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SesionResponse>> Login(IniciarSesionRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.Activo)
        {
            return Unauthorized();
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Contrasena, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return Unauthorized();
        }

        return Ok(await CreateSessionAsync(user));
    }

    private async Task<SesionResponse> CreateSessionAsync(UsuarioSistema user)
    {
        var (token, expiresAt) = await tokenIssuer.CreateAsync(user, userManager);
        var roles = (await userManager.GetRolesAsync(user)).OrderBy(role => role, StringComparer.Ordinal).ToArray();
        return new SesionResponse(
            token,
            expiresAt,
            user.Id,
            user.NombreCompleto,
            user.Email ?? string.Empty,
            roles);
    }

    private ActionResult IdentityValidationProblem(IdentityResult result)
    {
        var errors = result.Errors.Select(error => error.Description).ToArray();
        return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["identity"] = errors }));
    }
}
