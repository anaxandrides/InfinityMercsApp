using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.Retirement;

namespace InfinityMercsApp.UnitTests.Season.Retirement;

public sealed class TrooperRetirementTests
{
    private static SeasonFile Season() => new()
    {
        Rounds = [new SeasonRound { RoundIndex = 1 }, new SeasonRound { RoundIndex = 2 }],
        UnitGear = new Dictionary<int, List<SeasonUnitGear>>
        {
            [1] = [new SeasonUnitGear { Slot = GearSlot.Sidearm.Key(), ItemName = "Pistol" }],
            [2] = [new SeasonUnitGear { Slot = GearSlot.Armor.Key(), ItemName = "Light Armour (LI)" }],
            [3] = [new SeasonUnitGear { Slot = GearSlot.Sidearm.Key(), ItemName = "Spitfire" }]
        }
    };

    [Fact]
    public void Apply_Trooper_KeepsRenownAndGivesUpGear()
    {
        var season = Season();

        TrooperRetirement.Apply(season, new RetiringTrooper(2, "Bolt", 27, []));

        var retired = Assert.Single(season.Retired);
        Assert.Equal((2, "Bolt", 27, 2), (retired.EntryIndex, retired.Name, retired.Renown, retired.AfterRound));
        Assert.True(season.IsRetired(2));
        Assert.False(season.IsRetired(1));
        Assert.Equal([1, 3], season.UnitGear.Keys.Order());
        Assert.Equal(27, season.RetiredRenown);
    }

    [Fact]
    public void Apply_TrooperWithPeripherals_TheyLeaveWithoutRenown()
    {
        var season = Season();

        TrooperRetirement.Apply(season, new RetiringTrooper(1, "Doctor", 30, [new RetiringPeripheral(3, "Traktor Mul")]));

        Assert.Equal([1, 3], season.Retired.Select(trooper => trooper.EntryIndex));
        Assert.Equal(0, season.Retired[1].Renown);
        Assert.Equal([2], season.UnitGear.Keys);
        Assert.Equal(30, season.RetiredRenown);
    }
}
