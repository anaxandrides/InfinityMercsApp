using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Domain.Models.Company;
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
            new SavedCompanyEntry { EntryIndex = 2, Name = "Bolt", Cost = 20, IsLieutenant = true, CarriesCaptainUpgrades = true },
            new SavedCompanyEntry { EntryIndex = 3, Name = "Traktor Mul", Cost = 0, IsPeripheralUnit = true, ExperiencePoints = 7 },
            new SavedCompanyEntry { EntryIndex = 4, Name = "Drumbot", Cost = 0, IsPeripheralUnit = true, ExperiencePoints = 5 }
        ]
    };

    // The Traktor Mul is a Servant; the Drumbot a trooper of its own.
    private static readonly IReadOnlySet<int> Servants = new HashSet<int> { 3 };

    [Fact]
    public void Of_Trooper_CountsItsSeasonGearAndXp()
    {
        var season = new SeasonFile
        {
            Rounds = [new SeasonRound { RoundIndex = 1, MissionResults = new SeasonMissionResult { UnitResults = [new() { EntryIndex = 1, XpGained = 6 }] } }]
        };
        season.UnitGear[1] = [new SeasonUnitGear { ItemName = "Pistol", Slot = GearSlot.Sidearm.Key() }];

        var renown = SeasonRenown.Of(Company().Entries[0], Company(), season, Catalog, Servants);

        Assert.Equal(new(InitialCost: 10, GearCost: 3, Experience: 4 + 6), renown);
    }

    [Fact]
    public void OfCompany_ServantsAndWins_LeavesOutServantsAndCountsWins()
    {
        var season = new SeasonFile { Rounds = [new SeasonRound { RoundIndex = 1, MissionResults = new SeasonMissionResult { Won = true } }] };

        Assert.Equal(14 + 20 + 5 + 10, SeasonRenown.OfCompany(Company(), season, Catalog, Servants));
    }

    [Fact]
    public void Of_OwnerOfARecruitedServant_CountsTheServantsCost()
    {
        var company = WithRecruitedPalbot();

        var renown = SeasonRenown.Of(company.Entries[0], company, null, Catalog, new HashSet<int> { 3, 5 });

        Assert.Equal(10 + 4, renown.InitialCost);
    }

    [Fact]
    public void OfCompany_RecruitedServant_CountsOnceThroughItsOwner()
    {
        var company = WithRecruitedPalbot();

        Assert.Equal(14 + 4 + 20 + 5, SeasonRenown.OfCompany(company, null, Catalog, new HashSet<int> { 3, 5 }));
    }

    // The Fusilier is a Doctor who bought a Palbot on its own.
    private static SavedCompanyFile WithRecruitedPalbot()
    {
        var company = Company();
        company.Entries.Add(new SavedCompanyEntry { EntryIndex = 5, Name = "Palbot", Cost = 4, IsPeripheralUnit = true, ParentEntryIndex = 1 });
        return company;
    }
}
