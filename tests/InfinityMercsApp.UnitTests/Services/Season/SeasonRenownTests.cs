using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.UnitTests.Season.Inventory;

namespace InfinityMercsApp.UnitTests.Services.Season;

public sealed class SeasonRenownTests
{
    private static readonly InventoryCatalog Catalog = InventoryCatalog.From([InventoryTestStores.NumberOne]);

    private static SavedCompanyFile Company() => new()
    {
        Entries =
        [
            new SavedCompanyEntry { EntryIndex = 1, Name = "Fusilier", Cost = 10, ExperiencePoints = 4 },
            new SavedCompanyEntry { EntryIndex = 2, Name = "Bolt", Cost = 20, IsLieutenant = true },
            new SavedCompanyEntry { EntryIndex = 3, Name = "Traktor Mul", Cost = 0, IsPeripheralUnit = true, ExperiencePoints = 7 }
        ]
    };

    [Fact]
    public void ATrooperCountsItsSeasonGearAndXp()
    {
        var season = new SeasonFile
        {
            Rounds = [new SeasonRound { RoundIndex = 1, MissionResults = new SeasonMissionResult { UnitResults = [new() { EntryIndex = 1, XpGained = 6 }] } }]
        };
        season.UnitGear[1] = [new SeasonUnitGear { ItemName = "Pistol", Slot = GearSlot.Sidearm.Key() }];

        var renown = SeasonRenown.Of(Company().Entries[0], season, Catalog);

        Assert.Equal(new(InitialCost: 10, GearCost: 3, Experience: 4 + 6), renown);
    }

    [Fact]
    public void TheCompanyLeavesOutPeripheralsAndCountsWins()
    {
        var season = new SeasonFile { Rounds = [new SeasonRound { RoundIndex = 1, MissionResults = new SeasonMissionResult { Won = true } }] };

        Assert.Equal(14 + 20 + 10, SeasonRenown.OfCompany(Company(), season, Catalog));
    }
}
