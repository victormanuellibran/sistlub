using SistLubri.Application.Clientes;
using SistLubri.Application.Common.Exceptions;
using SistLubri.Application.Tests.Support;
using SistLubri.Domain.Clientes;

namespace SistLubri.Application.Tests.Clientes;

public sealed class ClienteServiceTests
{
    [Fact]
    public async Task Create_AddsAValidClientAndSavesChanges()
    {
        var clientes = new FakeClienteRepository();
        var vehicles = new FakeVehiculoRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = new ClienteService(clientes, vehicles, unitOfWork);

        var result = await service.CreateAsync(
            new CrearClienteCommand("Ana", "Gomez", "351 555-0101", null, null, null));

        Assert.Single(clientes.Items);
        Assert.Equal("3515550101", result.Telefono);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_RejectsAnExactDuplicate()
    {
        var existing = new Cliente("Ana", "Gomez", "3515550101");
        var service = new ClienteService(
            new FakeClienteRepository(existing),
            new FakeVehiculoRepository(),
            new FakeUnitOfWork());

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(
            new CrearClienteCommand("ana", "gomez", "351-555-0101", null, null, null)));
    }
}
