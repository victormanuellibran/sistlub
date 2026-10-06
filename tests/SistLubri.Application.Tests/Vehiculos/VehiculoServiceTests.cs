using SistLubri.Application.Common.Exceptions;
using SistLubri.Application.Tests.Support;
using SistLubri.Application.Vehiculos;
using SistLubri.Domain.Clientes;

namespace SistLubri.Application.Tests.Vehiculos;

public sealed class VehiculoServiceTests
{
    [Fact]
    public async Task Create_AssociatesTheVehicleWithAnActiveClient()
    {
        var cliente = new Cliente("Ana", "Gomez", "3515550101");
        var vehicles = new FakeVehiculoRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = new VehiculoService(
            vehicles,
            new FakeClienteRepository(cliente),
            unitOfWork);

        var result = await service.CreateAsync(
            cliente.Id,
            new CrearVehiculoCommand("ab-123-cd", "Toyota", "Corolla", 2022, 42_000, 50_000, null));

        Assert.Single(vehicles.Items);
        Assert.Equal("AB123CD", result.Patente);
        Assert.Equal(cliente.Id, result.ClienteId);
        Assert.Equal(50_000, result.ProximoCambioKilometraje);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_RejectsAnInactiveClient()
    {
        var cliente = new Cliente("Ana", "Gomez", "3515550101");
        cliente.Desactivar();
        var service = new VehiculoService(
            new FakeVehiculoRepository(),
            new FakeClienteRepository(cliente),
            new FakeUnitOfWork());

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(
            cliente.Id,
            new CrearVehiculoCommand("AB123CD", "Toyota", "Corolla", null, null, null, null)));
    }
}
