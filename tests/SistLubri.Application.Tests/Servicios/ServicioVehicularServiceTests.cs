using SistLubri.Application.Common.Exceptions;
using SistLubri.Application.Servicios;
using SistLubri.Application.Tests.Support;
using SistLubri.Domain.Clientes;
using SistLubri.Domain.Productos;
using SistLubri.Domain.Servicios;
using SistLubri.Domain.Vehiculos;

namespace SistLubri.Application.Tests.Servicios;

public sealed class ServicioVehicularServiceTests
{
    [Fact]
    public async Task Finalize_ConsumesStockAndUpdatesMileageForTheMatchingPlate()
    {
        var client = new Cliente("Ana", "Gomez", "3515550101");
        var vehicle = new Vehiculo(client.Id, "AB123CD", "Toyota", "Corolla", 2020, 45_000, 55_000);
        var unrelatedClient = new Cliente("Luis", "Lopez", "3515550102");
        var unrelatedVehicle = new Vehiculo(
            unrelatedClient.Id,
            "XY987ZT",
            "Ford",
            "Focus",
            2019,
            210_000,
            220_000);
        var product = CreateProduct(stock: 10m);
        var services = new FakeServicioVehicularRepository();
        var vehicles = new FakeVehiculoRepository(vehicle, unrelatedVehicle);
        var products = new FakeProductoRepository(product);
        var unitOfWork = new FakeUnitOfWork();
        var service = new ServicioVehicularService(
            services,
            vehicles,
            new FakeClienteRepository(client, unrelatedClient),
            products,
            unitOfWork);

        var created = await service.CreateAsync(new CrearServicioVehicularCommand(
            "AB-123-CD",
            48_000,
            "Cambio de aceite y filtro",
            "Inspeccion inicial",
            null,
            null,
            12_000m,
            [new CrearDetalleServicioCommand(product.Id, 3m)]));

        Assert.Equal(10m, product.StockActual);
        Assert.Equal(vehicle.Id, created.VehiculoId);
        Assert.Equal(client.Id, created.ClienteId);
        Assert.Equal("AB123CD", created.Patente);

        await service.UpdateDetailsAsync(
            created.Id,
            [new CrearDetalleServicioCommand(product.Id, 2m)]);
        var finalized = await service.FinalizeAsync(created.Id, 49_000, 59_000);

        Assert.Equal(EstadoServicio.Finalizado, finalized.Estado);
        Assert.Equal(14_000m, finalized.Total);
        Assert.Equal(8m, product.StockActual);
        Assert.Equal(49_000, vehicle.KilometrajeActual);
        Assert.Equal(59_000, vehicle.ProximoCambioKilometraje);
        Assert.Equal(2m, Assert.Single(finalized.Detalles).Cantidad);
        Assert.Single(product.Movimientos, movement => movement.Tipo == TipoMovimientoInventario.Salida);
        Assert.Equal(created.Id, product.Movimientos.Last().ReferenciaId);
        Assert.Equal(3, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_RejectsMileageLowerThanTheLastValueForTheSamePlate()
    {
        var client = new Cliente("Ana", "Gomez", "3515550101");
        var vehicle = new Vehiculo(client.Id, "AB123CD", "Toyota", "Corolla", 2020, 45_000, 55_000);
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(vehicle, client, unitOfWork: unitOfWork);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(new CrearServicioVehicularCommand(
            "AB123CD",
            44_999,
            "Cambio de aceite",
            null,
            null,
            null,
            0m,
            [])));

        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_ComparesMileageOnlyAgainstTheVehicleWithThatPlate()
    {
        var client = new Cliente("Ana", "Gomez", "3515550101");
        var targetVehicle = new Vehiculo(client.Id, "AB123CD", "Toyota", "Corolla", 2020, 4_000, 9_000);
        var otherClient = new Cliente("Luis", "Lopez", "3515550102");
        var otherVehicle = new Vehiculo(otherClient.Id, "XY987ZT", "Ford", "Focus", 2019, 210_000, 220_000);
        var service = new ServicioVehicularService(
            new FakeServicioVehicularRepository(),
            new FakeVehiculoRepository(targetVehicle, otherVehicle),
            new FakeClienteRepository(client, otherClient),
            new FakeProductoRepository(),
            new FakeUnitOfWork());

        var result = await service.CreateAsync(new CrearServicioVehicularCommand(
            "AB123CD",
            4_500,
            "Cambio de aceite",
            null,
            null,
            null,
            0m,
            []));

        Assert.Equal(targetVehicle.Id, result.VehiculoId);
        Assert.Equal("AB123CD", result.Patente);
    }

    private static ServicioVehicularService CreateService(
        Vehiculo vehicle,
        Cliente client,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new ServicioVehicularService(
            new FakeServicioVehicularRepository(),
            new FakeVehiculoRepository(vehicle),
            new FakeClienteRepository(client),
            new FakeProductoRepository(),
            unitOfWork ?? new FakeUnitOfWork());
    }

    private static Producto CreateProduct(decimal stock)
    {
        return new Producto(
            "ACE-001",
            "Aceite 10W40",
            "Marca",
            "Aceites",
            "Bidon 4 L",
            UnidadMedidaProducto.Unidad,
            1_000m,
            700m,
            false,
            stock);
    }
}
