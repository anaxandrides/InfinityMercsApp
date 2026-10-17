using InfinityMercsApp.Domain.Models.Season.Gear;

namespace InfinityMercsApp.UnitTests.Season;

public sealed class CompanyLoadoutsTests
{
    [Fact]
    public void ACopyTakenFromAnotherTrooperLeavesThem()
    {
        var loadouts = new CompanyLoadouts();
        loadouts.Assign(1, GearSlot.Primary, "Spitfire");

        loadouts.Assign(2, GearSlot.Primary, "spitfire", takenFrom: 1);

        Assert.Null(loadouts.For(1)[GearSlot.Primary]);
        Assert.Equal("spitfire", loadouts.For(2)[GearSlot.Primary]);
    }

    // A company with two Hacking Devices can give one to each of two troopers.
    [Fact]
    public void ASpareCopyLeavesTheOthersWhereTheyAre()
    {
        var loadouts = new CompanyLoadouts();
        loadouts.Assign(1, GearSlot.Accessories, "Hacking Device");

        loadouts.Assign(2, GearSlot.Accessories, "Hacking Device");

        Assert.Equal([new GearHolding(1, GearSlot.Accessories), new GearHolding(2, GearSlot.Accessories)], loadouts.Holding("Hacking Device"));
    }

    [Fact]
    public void TheSameNameInAnotherSlotIsLeftAlone()
    {
        var loadouts = new CompanyLoadouts();
        loadouts.Assign(1, GearSlot.Secondary, "Pistol");

        loadouts.Assign(2, GearSlot.Sidearm, "Pistol", takenFrom: 1);

        Assert.Equal("Pistol", loadouts.For(1)[GearSlot.Secondary]);
    }

    [Fact]
    public void AssigningNothingEmptiesTheSlot()
    {
        var loadouts = new CompanyLoadouts();
        loadouts.Assign(1, GearSlot.Armor, "Kevlar");

        loadouts.Assign(1, GearSlot.Armor, null);

        Assert.Null(loadouts.For(1)[GearSlot.Armor]);
    }

    [Fact]
    public void FindsEveryCopyCarried()
    {
        var loadouts = new CompanyLoadouts();
        loadouts.Assign(4, GearSlot.Roles, "Medic");
        loadouts.Assign(2, GearSlot.Extra, "medic");

        Assert.Equal([new GearHolding(2, GearSlot.Extra), new GearHolding(4, GearSlot.Roles)], loadouts.Holding("Medic"));
        Assert.Empty(loadouts.Holding("Doctor"));
    }

    [Fact]
    public void RoundTripsThroughTheSeasonFile()
    {
        var loadouts = CompanyLoadouts.From(new Dictionary<int, List<SeasonUnitGear>>
        {
            [3] = [new SeasonUnitGear { Slot = "Armor", ItemName = "Kevlar" }]
        });
        loadouts.Assign(5, GearSlot.Augments, "WIP=12");

        var saved = loadouts.ToSeasonGear();

        Assert.Equal("Kevlar", saved[3].Single().ItemName);
        Assert.Equal("Augments", saved[5].Single().Slot);
    }
}
