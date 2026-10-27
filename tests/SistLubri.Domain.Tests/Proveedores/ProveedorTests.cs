using SistLubri.Domain.Common;
using SistLubri.Domain.Proveedores;

namespace SistLubri.Domain.Tests.Proveedores;

public sealed class ProveedorTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Crear_NormalizaCuitTelefonoYCorreo()
    {
        var proveedor = Crear(cuit: "30-12345678-9", telefono: "+54 (11) 5555-0101", email: " COMPRAS@ACEITE.COM ");

        Assert.Equal("30123456789", proveedor.Cuit);
        Assert.Equal("541155550101", proveedor.Telefono);
        Assert.Equal("compras@aceite.com", proveedor.Email);
        Assert.True(proveedor.Activo);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("30-12345678-X")]
    public void Crear_RechazaCuitConFormatoInvalido(string cuit)
    {
        Assert.Throws<DomainException>(() => Crear(cuit: cuit));
    }

    [Fact]
    public void Desactivar_ExigeMotivoYConservaHistorialAlReactivar()
    {
        var proveedor = Crear();
        Assert.Throws<DomainException>(() => proveedor.Desactivar(" ", UserId));

        proveedor.Desactivar("Cese de operaciones", UserId);
        proveedor.Reactivar(UserId);

        Assert.True(proveedor.Activo);
        Assert.Collection(proveedor.HistorialEstados,
            entry => { Assert.Equal(EstadoProveedor.Inactivo, entry.Estado); Assert.Equal("Cese de operaciones", entry.Motivo); },
            entry => { Assert.Equal(EstadoProveedor.Activo, entry.Estado); Assert.Null(entry.Motivo); });
    }

    [Fact]
    public void ActualizarProveedorInactivo_SoloPermiteContactoDireccionYObservaciones()
    {
        var proveedor = Crear();
        proveedor.Desactivar("No opera temporalmente", UserId);

        proveedor.ActualizarDatos("Proveedor Uno", null, "Contacto nuevo", "11-5555-0102",
            "nuevo@proveedor.com", "Calle 123", "Aceites y filtros", "Cuenta corriente", 30,
            "Fin de mes", "Llamar por la tarde", UserId);

        Assert.Equal("Contacto nuevo", proveedor.ContactoNombre);
        Assert.Equal("Calle 123", proveedor.Direccion);
        Assert.Throws<DomainException>(() => proveedor.ActualizarDatos("Otro nombre", null, null,
            "1155550102", null, null, null, null, null, null, null, UserId));
    }

    [Fact]
    public void Crear_ExigeNombreTelefonoYUsuarioResponsable()
    {
        Assert.Throws<DomainException>(() => Crear(razonSocial: " "));
        Assert.Throws<DomainException>(() => Crear(telefono: "123"));
        Assert.Throws<DomainException>(() => Crear(usuarioId: Guid.Empty));
    }

    private static Proveedor Crear(
        string razonSocial = "Proveedor Uno",
        string? cuit = null,
        string telefono = "11 5555-0101",
        string? email = null,
        Guid? usuarioId = null) =>
        new(razonSocial, cuit, "Contacto", telefono, email, "Calle 123", "Aceites y filtros",
            "Cuenta corriente", 30, "Fin de mes", null, usuarioId ?? UserId);
}
