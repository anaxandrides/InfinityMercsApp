using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Presentation.Units;

namespace InfinityMercsApp.UnitTests.Presentation.Units;

public sealed class UnitHeaderIconsTests
{
    private const string Folder = "SVGCache/CBIcons/";

    [Fact]
    public void NoIconsShowNothing()
    {
        Assert.Empty(UnitHeaderIcons.None.Sources);
        Assert.False(UnitHeaderIcons.None.HasAny);
    }

    [Fact]
    public void IconsAreInColumnOrder()
    {
        var icons = new UnitHeaderIcons { Hackable = true, Cube = true, Regular = true, Peripheral = true, Lieutenants = 1 };

        Assert.Equal(
            [Folder + "regular.svg", Folder + "lieutenant.svg", Folder + "peripheral.svg", Folder + "cube.svg", Folder + "hackable.svg"],
            icons.Sources);
        Assert.True(icons.HasAny);
    }

    [Fact]
    public void IrregularReplacesRegular() =>
        Assert.Equal([Folder + "irregular.svg"], new UnitHeaderIcons { Regular = true, Irregular = true }.Sources);

    [Fact]
    public void EachLieutenantOrderHasAnIcon() =>
        Assert.Equal(3, new UnitHeaderIcons { Lieutenants = 3 }.Sources.Count(source => source.EndsWith("lieutenant.svg")));

    [Fact]
    public void TheColumnHoldsSixIcons()
    {
        var icons = new UnitHeaderIcons
        {
            Regular = true, Lieutenants = 2, Peripheral = true, Impetuous = true, TacticalAwareness = true,
            Cube = true, Cube2 = true, Hackable = true
        };

        Assert.Equal(UnitHeaderIcons.MaxShown, icons.Sources.Count);
        Assert.Equal(Folder + "tactical.svg", icons.Sources[^1]);
    }

    [Fact]
    public void ASavedTroopersTraitsAndLieutenantOrdersBecomeIcons()
    {
        var icons = UnitHeaderIcons.From(new UnitTraitIcons(true, false, true, false, true, true, true), lieutenants: 2);

        Assert.Equal(
            new UnitHeaderIcons
            {
                Regular = true, TacticalAwareness = true, Cube2 = true, Hackable = true, Peripheral = true, Lieutenants = 2
            },
            icons);
    }

    [Fact]
    public void TheTooltipSaysWhatEachIconMeansInColumnOrder()
    {
        var icons = new UnitHeaderIcons { Regular = true, Lieutenants = 1, Hackable = true };

        Assert.Equal(
            ["Regular: adds a Regular order to the order pool", "Lieutenant: gives a Lieutenant order", "Hackable: can be targeted by hacking programs"],
            icons.Meanings.Split(Environment.NewLine));
        Assert.Equal(icons.Shown.Select(icon => icon.Source), icons.Sources);
    }
}
