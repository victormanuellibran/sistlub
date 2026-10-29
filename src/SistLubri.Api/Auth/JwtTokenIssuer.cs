using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SistLubri.Infrastructure.Persistence.Usuarios;

namespace SistLubri.Api.Auth;

public sealed class JwtTokenIssuer(JwtSettings settings)
{
    public async Task<(string Token, DateTimeOffset ExpiresAtUtc)> CreateAsync(
        UsuarioSistema user,
        UserManager<UsuarioSistema> userManager)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.Add(settings.Lifetime);
        var roles = await userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.NombreCompleto),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(settings.SigningKey),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            settings.Issuer,
            settings.Audience,
            claims,
            now.UtcDateTime,
            expiresAt.UtcDateTime,
            credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
