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
    public void IsServant_SkillLines_TrueForServantAndAncillary(IReadOnlyList<string> lines, bool isServant) =>
        Assert.Equal(isServant, PeripheralKinds.IsServant(lines));

    [Fact]
    public void AsTrooper_RegularPeripheral_BecomesIrregular() =>
        Assert.Equal(["Cube", "Irregular"], PeripheralKinds.AsTrooper(["Regular", "Cube"]));
}
