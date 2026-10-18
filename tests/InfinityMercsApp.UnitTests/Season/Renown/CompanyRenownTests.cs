using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Domain.Season.Renown;
using InfinityMercsApp.UnitTests.Season.Inventory;

namespace InfinityMercsApp.UnitTests.Season.Renown;

public sealed class CompanyRenownTests
{
    private static readonly InventoryCatalog Catalog = InventoryCatalog.From([InventoryTestStores.NumberOne]);

    [Fact]
    public void Total_Trooper_IsCostGearAndXp() =>
        Assert.Equal(28 + 15 + 9, new TrooperRenown(InitialCost: 28, GearCost: 15, Experience: 9).Total);

    [Fact]
    public void WithoutGear_RetiringTrooper_KeepsCostAndXp() =>
        Assert.Equal(28 + 9, new TrooperRenown(InitialCost: 28, GearCost: 15, Experience: 9).WithoutGear);

    [Fact]
    public void Of_RetiredTroopers_StillCount()
    {
        var season = new SeasonFile { Retired = [new RetiredTrooper { EntryIndex = 4, Renown = 31 }] };

        Assert.Equal(20 + 31, CompanyRenown.Of([new TrooperRenown(20, 0, 0)], season));
    }

    [Fact]
    public void Total_NegativeParts_AreNotCounted() =>
        Assert.Equal(5, new TrooperRenown(InitialCost: -3, GearCost: 0, Experience: 5).Total);

    [Fact]
    public void GearCost_Gear_CountsItsBaseCost()
    {
        List<SeasonUnitGear> gear =
        [
            new() { ItemName = "Combi Rifle", Slot = GearSlot.Primary.Key() },
            new() { ItemName = "WIP=12", Slot = GearSlot.Augments.Key() },
            new() { ItemName = "Nobody Sells This", Slot = GearSlot.Accessories.Key() },
            new() { ItemName = " ", Slot = GearSlot.Sidearm.Key() }
        ];

        Assert.Equal(12 + 15, CompanyRenown.GearCost(gear, Catalog));
    }

    [Fact]
    public void Of_WinsAndBye_AddsTenPerWinNoneForTheBye()
    {
        var season = new SeasonFile
        {
            Rounds =
            [
                new SeasonRound { RoundIndex = 1, MissionResults = new SeasonMissionResult { Won = true } },
                new SeasonRound { RoundIndex = 2, MissionResults = new SeasonMissionResult { Won = false } },
                new SeasonRound { RoundIndex = 3, MissionResults = new SeasonMissionResult { Won = true } },
                new SeasonRound { RoundIndex = 4, MissionResults = new SeasonMissionResult { Won = true, IsBye = true } }
            ]
        };

        Assert.Equal(2, CompanyRenown.Victories(season));
        Assert.Equal(30 + 12 + 2 * CompanyRenown.VictoryRenown,
            CompanyRenown.Of([new TrooperRenown(30, 0, 0), new TrooperRenown(10, 0, 2)], season));
    }

    [Fact]
    public void Of_NoSeason_CountsOnlyTheTroopers() =>
        Assert.Equal(40, CompanyRenown.Of([new TrooperRenown(30, 0, 10)], null));
}
