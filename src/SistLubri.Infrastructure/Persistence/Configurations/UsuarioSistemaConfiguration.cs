using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistLubri.Infrastructure.Persistence.Usuarios;

namespace SistLubri.Infrastructure.Persistence.Configurations;

internal sealed class UsuarioSistemaConfiguration : IEntityTypeConfiguration<UsuarioSistema>
{
    public void Configure(EntityTypeBuilder<UsuarioSistema> builder)
    {
        builder.Property(usuario => usuario.NombreCompleto).HasMaxLength(120).IsRequired();
        builder.HasIndex(usuario => usuario.Activo);
    }
}
