using System.Security.Claims;
using SistLubri.Application.Common.Security;

namespace SistLubri.Api.Auth;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string Nombre => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name)
        ?? throw new UnauthorizedAccessException("No se pudo identificar el usuario autenticado.");

    public Guid Id
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var userId)
                ? userId
                : throw new UnauthorizedAccessException("No se pudo identificar al usuario autenticado.");
        }
    }
}
