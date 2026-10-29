using Microsoft.AspNetCore.Identity;

namespace SistLubri.Infrastructure.Persistence.Usuarios;

public sealed class UsuarioSistema : IdentityUser<Guid>
{
    public string NombreCompleto { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public DateTimeOffset CreadoEnUtc { get; set; } = DateTimeOffset.UtcNow;
}
