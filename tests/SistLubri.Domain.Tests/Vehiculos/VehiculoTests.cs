using SistLubri.Domain.Common;
using SistLubri.Domain.Vehiculos;

namespace SistLubri.Domain.Tests.Vehiculos;

public sealed class VehiculoTests
{
    [Fact]
    public void Create_NormalizesPatente()
    {
        var vehiculo = new Vehiculo(
            Guid.NewGuid(),
            " ab-123-cd ",
            "Toyota",
            "Corolla",
            2020,
            50_000,
            60_000);

        Assert.Equal("AB123CD", vehiculo.Patente);
        Assert.Equal(50_000, vehiculo.KilometrajeActual);
        Assert.Equal(60_000, vehiculo.ProximoCambioKilometraje);
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("AB@123CD")]
    public void Create_RejectsInvalidPatente(string patente)
    {
        Assert.Throws<DomainException>(() =>
            new Vehiculo(Guid.NewGuid(), patente, "Toyota", "Corolla"));
    }

    [Fact]
    public void ChangeOwner_RejectsAnEmptyClientId()
    {
        var vehiculo = new Vehiculo(
            Guid.NewGuid(),
            "AB123CD",
            "Toyota",
            "Corolla");

        Assert.Throws<DomainException>(() => vehiculo.CambiarTitular(Guid.Empty));
    }

    [Fact]
    public void Create_RejectsANegativeNextServiceMileage()
    {
        Assert.Throws<DomainException>(() =>
            new Vehiculo(Guid.NewGuid(), "AB123CD", "Toyota", "Corolla", 2020, 50_000, -1));
    }

    [Fact]
    public void Update_RejectsLowerCurrentMileage()
    {
        var vehiculo = new Vehiculo(
            Guid.NewGuid(),
            "AB123CD",
            "Toyota",
            "Corolla",
            2020,
            50_000,
            60_000);

        Assert.Throws<DomainException>(() => vehiculo.ActualizarDatos(
            "Toyota",
            "Corolla",
            2020,
            49_999,
            60_000,
            null));
    }

    [Fact]
    public void Create_RejectsNextServiceMileageNotAfterCurrentMileage()
    {
        Assert.Throws<DomainException>(() =>
            new Vehiculo(Guid.NewGuid(), "AB123CD", "Toyota", "Corolla", 2020, 50_000, 50_000));
    }
}
