using SistLubri.Domain.Clientes;
using SistLubri.Domain.Common;

namespace SistLubri.Domain.Tests.Clientes;

public sealed class ClienteTests
{
    [Fact]
    public void Create_NormalizesContactDataAndStartsActive()
    {
        var cliente = new Cliente(
            "  Ana ",
            " Gomez  ",
            "+54 (351) 555-0101",
            " ANA@EXAMPLE.COM ");

        Assert.Equal("Ana", cliente.Nombre);
        Assert.Equal("Gomez", cliente.Apellido);
        Assert.Equal("543515550101", cliente.Telefono);
        Assert.Equal("ana@example.com", cliente.Email);
        Assert.True(cliente.Activo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("351-ABC-123")]
    public void Create_RejectsInvalidPhone(string telefono)
    {
        Assert.Throws<DomainException>(() => new Cliente("Ana", "Gomez", telefono));
    }

    [Fact]
    public void DeactivateAndReactivate_PreserveTheClientAndChangeItsState()
    {
        var cliente = new Cliente("Ana", "Gomez", "3515550101");

        cliente.Desactivar();

        Assert.False(cliente.Activo);
        Assert.NotNull(cliente.DesactivadoEnUtc);

        cliente.Reactivar();

        Assert.True(cliente.Activo);
        Assert.Null(cliente.DesactivadoEnUtc);
    }
}
