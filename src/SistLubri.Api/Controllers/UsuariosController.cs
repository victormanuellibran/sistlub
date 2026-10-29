using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistLubri.Api.Auth;
using SistLubri.Api.Contracts.Auth;
using SistLubri.Application.Common.Security;
using SistLubri.Infrastructure.Persistence;
using SistLubri.Infrastructure.Persistence.Usuarios;

namespace SistLubri.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = Roles.Administrator)]
public sealed class UsuariosController(
    UserManager<UsuarioSistema> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    SistLubriDbContext dbContext,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UsuarioResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UsuarioResponse>>> List(CancellationToken cancellationToken)
    {
        var users = await userManager.Users
            .AsNoTracking()
            .OrderBy(user => user.NombreCompleto)
            .Take(200)
            .ToListAsync(cancellationToken);
        var responses = new List<UsuarioResponse>(users.Count);
        foreach (var user in users)
        {
            var roles = (await userManager.GetRolesAsync(user)).OrderBy(role => role, StringComparer.Ordinal).ToArray();
            responses.Add(Map(user, roles));
        }

        return Ok(responses);
    }

    [HttpPost]
    [ProducesResponseType<UsuarioResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioResponse>> Create(
        CrearUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var role = request.Rol.Trim();
        if (!Roles.All.Contains(role, StringComparer.Ordinal) || !await roleManager.RoleExistsAsync(role))
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.Rol)] = ["El rol solicitado no es valido."]
            }));
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
            return IdentityProblem(createResult);
        }

        var roleResult = await userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return IdentityProblem(roleResult);
        }

        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        var response = Map(user, roles);
        return CreatedAtAction(nameof(List), new { id = user.Id }, response);
    }

    [HttpPut("{id:guid}/estado")]
    [ProducesResponseType<UsuarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioResponse>> SetActive(
        Guid id,
        CambiarEstadoUsuarioRequest request)
    {
        if (id == currentUser.Id && !request.Activo)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "No puede desactivar su propia cuenta."
            });
        }

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        user.Activo = request.Activo;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return IdentityProblem(result);
        }

        return Ok(Map(user, (await userManager.GetRolesAsync(user)).ToArray()));
    }

    [HttpPut("{id:guid}/rol")]
    [ProducesResponseType<UsuarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioResponse>> ChangeRole(
        Guid id,
        CambiarRolUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var role = request.Rol.Trim();
        if (!Roles.All.Contains(role, StringComparer.Ordinal) || !await roleManager.RoleExistsAsync(role))
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.Rol)] = ["El rol solicitado no es valido."]
            }));
        }

        if (id == currentUser.Id && role != Roles.Administrator)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "No puede quitarse el rol de administrador a si mismo."
            });
        }

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var existingRoles = await userManager.GetRolesAsync(user);
        var removeResult = await userManager.RemoveFromRolesAsync(user, existingRoles);
        if (!removeResult.Succeeded)
        {
            return IdentityProblem(removeResult);
        }

        var addResult = await userManager.AddToRoleAsync(user, role);
        if (!addResult.Succeeded)
        {
            return IdentityProblem(addResult);
        }

        await transaction.CommitAsync(cancellationToken);
        return Ok(Map(user, [role]));
    }

    private static UsuarioResponse Map(UsuarioSistema user, IReadOnlyList<string> roles)
    {
        return new UsuarioResponse(user.Id, user.NombreCompleto, user.Email ?? string.Empty, user.Activo, roles);
    }

    private ActionResult IdentityProblem(IdentityResult result)
    {
        return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["identity"] = result.Errors.Select(error => error.Description).ToArray()
        }));
    }
}
