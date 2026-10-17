using InfinityMercsApp.Domain.Company;

namespace InfinityMercsApp.UnitTests.Company;

public sealed class PeripheralKindsTests
{
    public static TheoryData<IReadOnlyList<string>, bool> Kinds => new()
    {
        { ["Peripheral (Servant)"], true },
        { ["Courage", "Peripheral (Ancillary)"], true },
        { ["Peripheral (Synchronized)"], false },
        { ["Peripheral"], false },
        { [], false }
    };

    [Theory]
    [MemberData(nameof(Kinds))]
    public void ServantAndAncillaryPeripheralsAreEquipment(IReadOnlyList<string> lines, bool isServant) =>
        Assert.Equal(isServant, PeripheralKinds.IsServant(lines));

    [Fact]
    public void APeripheralTrooperIsIrregular() =>
        Assert.Equal(["Cube", "Irregular"], PeripheralKinds.AsTrooper(["Regular", "Cube"]));
}
