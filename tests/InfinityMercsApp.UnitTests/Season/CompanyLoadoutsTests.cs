using InfinityMercsApp.Domain.Models.Season.Gear;

namespace InfinityMercsApp.UnitTests.Season;

public sealed class CompanyLoadoutsTests
{
    [Fact]
    public void AssigningAnItemTakesItFromWhoeverHadIt()
    {
        var loadouts = new CompanyLoadouts();
        loadouts.Assign(1, GearSlot.Primary, "Spitfire");

        loadouts.Assign(2, GearSlot.Primary, "spitfire");

        Assert.Null(loadouts.For(1)[GearSlot.Primary]);
        Assert.Equal("spitfire", loadouts.For(2)[GearSlot.Primary]);
    }

    [Fact]
    public void TheSameNameInAnotherSlotIsLeftAlone()
    {
        var loadouts = new CompanyLoadouts();
        loadouts.Assign(1, GearSlot.Secondary, "Pistol");

        loadouts.Assign(2, GearSlot.Sidearm, "Pistol");

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
    public void FindsWhoElseHoldsAnItem()
    {
        var loadouts = new CompanyLoadouts();
        loadouts.Assign(4, GearSlot.Roles, "Medic");

        Assert.Equal(4, loadouts.HolderOf("Medic", GearSlot.Roles, exceptEntryIndex: 1));
        Assert.Null(loadouts.HolderOf("Medic", GearSlot.Roles, exceptEntryIndex: 4));
        Assert.Null(loadouts.HolderOf("Medic", GearSlot.Primary, exceptEntryIndex: 1));
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
