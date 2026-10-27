using SistLubri.Application.Common.Exceptions;
using SistLubri.Application.Proveedores;
using SistLubri.Application.Tests.Support;
using SistLubri.Domain.Proveedores;

namespace SistLubri.Application.Tests.Proveedores;

public sealed class ProveedorServiceTests
{
    [Fact]
    public async Task CreateAsync_GuardaProveedorYAuditaUsuario()
    {
        var repository = new FakeProveedorRepository();
        var unitOfWork = new FakeUnitOfWork();
        var user = new FakeCurrentUser();
        var service = new ProveedorService(repository, unitOfWork, user);

        var result = await service.CreateAsync(Command(cuit: "30-12345678-9"));

        var saved = Assert.Single(repository.Items);
        Assert.Equal("30123456789", saved.Cuit);
        Assert.Equal(user.Id, saved.CreadoPorUsuarioId);
        Assert.Equal(saved.Id, result.Id);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateAsync_RechazaCUITDuplicadoAunqueEsteFormateadoDistinto()
    {
        var repository = new FakeProveedorRepository(Proveedor());
        var service = new ProveedorService(repository, new FakeUnitOfWork(), new FakeCurrentUser());

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(Command(cuit: "30-12345678-9")));
        Assert.Single(repository.Items);
    }

    [Fact]
    public async Task CreateAsync_AdvierteCoincidenciaPeroPermiteConfirmarEntidadDistinta()
    {
        var existing = Proveedor(razonSocial: "Proveedor Uno", telefono: "1155550101");
        var repository = new FakeProveedorRepository(existing);
        var unitOfWork = new FakeUnitOfWork();
        var service = new ProveedorService(repository, unitOfWork, new FakeCurrentUser());

        var conflict = await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(Command()));
        Assert.Contains(existing.Id.ToString(), conflict.Message);
        Assert.Equal(0, unitOfWork.SaveCount);

        await service.CreateAsync(Command(confirmar: true));
        Assert.Equal(2, repository.Items.Count);
    }

    [Fact]
    public async Task UpdateAsync_RechazaCUITDeOtroProveedor()
    {
        var first = Proveedor(razonSocial: "Proveedor Uno", cuit: "30123456789");
        var second = Proveedor(razonSocial: "Proveedor Dos", cuit: "30765432109");
        var service = new ProveedorService(new FakeProveedorRepository(first, second),
            new FakeUnitOfWork(), new FakeCurrentUser());

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateAsync(second.Id,
            new ActualizarProveedorCommand("Proveedor Dos", "30-12345678-9", "Contacto", "11 5555-0102",
                "contacto@proveedor.com", "Calle 123", "Aceites y filtros", "Cuenta corriente", 30,
                "Fin de mes", null)));
    }

    [Fact]
    public async Task DeactivateAndReactivate_PersistenLosDosEventosDeEstado()
    {
        var proveedor = Proveedor();
        var service = new ProveedorService(new FakeProveedorRepository(proveedor),
            new FakeUnitOfWork(), new FakeCurrentUser());

        await service.DeactivateAsync(proveedor.Id, new DesactivarProveedorCommand("Proveedor dejó de operar"));
        var result = await service.ReactivateAsync(proveedor.Id);

        Assert.True(result.Activo);
        Assert.Collection(result.HistorialEstados,
            item => Assert.Equal(nameof(EstadoProveedor.Inactivo), item.Estado),
            item => Assert.Equal(nameof(EstadoProveedor.Activo), item.Estado));
    }

    [Fact]
    public async Task DeactivateAsync_SegundaBajaRespondeComoConflicto()
    {
        var proveedor = Proveedor();
        proveedor.Desactivar("Baja de prueba", Guid.NewGuid());
        var service = new ProveedorService(new FakeProveedorRepository(proveedor),
            new FakeUnitOfWork(), new FakeCurrentUser());

        await Assert.ThrowsAsync<ConflictException>(() => service.DeactivateAsync(proveedor.Id,
            new DesactivarProveedorCommand("Segundo intento")));
    }

    [Fact]
    public async Task ReactivateAsync_ProveedorActivoRespondeComoConflicto()
    {
        var proveedor = Proveedor();
        var service = new ProveedorService(new FakeProveedorRepository(proveedor),
            new FakeUnitOfWork(), new FakeCurrentUser());

        await Assert.ThrowsAsync<ConflictException>(() => service.ReactivateAsync(proveedor.Id));
    }

    private static CrearProveedorCommand Command(
        string razonSocial = "Proveedor Uno",
        string? cuit = null,
        string telefono = "11 5555-0101",
        bool confirmar = false) =>
        new(razonSocial, cuit, "Contacto", telefono, "contacto@proveedor.com", "Calle 123",
            "Aceites y filtros", "Cuenta corriente", 30, "Fin de mes", null, confirmar);

    private static Proveedor Proveedor(
        string razonSocial = "Proveedor Uno",
        string? cuit = null,
        string telefono = "11 5555-0101") =>
        new(razonSocial, cuit, "Contacto", telefono, "contacto@proveedor.com", "Calle 123",
            "Aceites y filtros", "Cuenta corriente", 30, "Fin de mes", null, Guid.NewGuid());
}
