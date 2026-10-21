using InfinityMercsApp.Domain.Company;

namespace InfinityMercsApp.UnitTests.Company;

public sealed class UnitTraitIconsTests
{
    [Fact]
    public void From_NoText_ShowsNoIcons() =>
        Assert.Equal(new UnitTraitIcons(false, false, false, false, false, false, false), UnitTraitIcons.From(null, null, null, false));

    [Fact]
    public void From_EveryText_ReadsIconsFromEach()
    {
        var icons = UnitTraitIcons.From("Regular, Cube", "Tactical Awareness", "Hacking Device", isPeripheralUnit: false);

        Assert.Equal(new UnitTraitIcons(true, false, true, true, false, true, false), icons);
    }

    [Theory]
    [InlineData("CUBE 2.0", false, true)]
    [InlineData("Cube2", false, true)]
    [InlineData("No Cube", false, false)]
    [InlineData("Cube", true, false)]
    public void From_Cube2OrNoCube_HidesThePlainCube(string characteristics, bool cube, bool cube2)
    {
        var icons = UnitTraitIcons.From(characteristics, null, null, false);

        Assert.Equal((cube, cube2), (icons.Cube, icons.Cube2));
    }

    [Theory]
    [InlineData("Hackable")]
    [InlineData("Killer Hacking Device")]
    [InlineData("KHD")]
    [InlineData("EVO HD")]
    public void From_HackingKit_IsHackable(string equipment) =>
        Assert.True(UnitTraitIcons.From(null, null, equipment, false).Hackable);

    [Theory]
    [InlineData("Servant", false, true)]
    [InlineData("Ancillary", false, true)]
    [InlineData("Irregular", true, true)]
    [InlineData("Irregular", false, false)]
    public void From_ServantAncillaryOrSavedPeripheral_ShowsPeripheral(string characteristics, bool isPeripheralUnit, bool expected) =>
        Assert.Equal(expected, UnitTraitIcons.From(characteristics, null, null, isPeripheralUnit).Peripheral);

    [Fact]
    public void From_Irregular_IsNotReadAsRegular()
    {
        var icons = UnitTraitIcons.From("Irregular", null, null, false);

        Assert.Equal((false, true), (icons.Regular, icons.Irregular));
    }
}
