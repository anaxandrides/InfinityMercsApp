using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.UnitTests.Season.Inventory;

namespace InfinityMercsApp.UnitTests.Services.Season;

public sealed class LoneWolfAspectsTests
{
    private static SavedCompanyFile ProxyPack(int startingXp = 0) => new()
    {
        CompanyType = "Proxy Pack",
        Entries =
        [
            new SavedCompanyEntry
            {
                EntryIndex = 3, Name = "Fusilier", BaseUnitName = "Fusilier", CustomName = "Kestrel", Cost = 12, CurrentBs = "13",
                IsLieutenant = true, CarriesCaptainUpgrades = true, IsLoneWolf = true, ExperiencePoints = startingXp
            }
        ]
    };

    [Fact]
    public void Sync_Unranked_HasOnlyTheLoneWolfsOwnLoadout()
    {
        var company = ProxyPack();

        Assert.False(LoneWolfAspects.Sync(company, season: null));

        Assert.Single(company.Entries);
    }

    [Fact]
    public void Sync_RankThree_AddsTwoAspectsCopiedFromTheLoneWolf()
    {
        var company = ProxyPack(startingXp: 30);

        Assert.True(LoneWolfAspects.Sync(company, season: null));

        var aspects = company.Entries.Skip(1).ToList();
        Assert.Equal([4, 5], aspects.Select(aspect => aspect.EntryIndex));
        Assert.Equal(["Kestrel (Aspect 2)", "Kestrel (Aspect 3)"], aspects.Select(aspect => aspect.CustomName));
        Assert.All(aspects, aspect =>
        {
            Assert.Equal(3, aspect.AspectOf);
            Assert.Equal(0, aspect.Cost);
            Assert.False(aspect.IsLieutenant);
            Assert.False(aspect.IsLoneWolf);
            Assert.True(aspect.CarriesCaptainUpgrades);
            Assert.Equal("13", aspect.CurrentBs);
        });
    }

    [Fact]
    public void Sync_AlreadyInStep_AddsNothing()
    {
        var company = ProxyPack(startingXp: 30);
        LoneWolfAspects.Sync(company, season: null);

        Assert.False(LoneWolfAspects.Sync(company, season: null));

        Assert.Equal(3, company.Entries.Count);
    }

    [Fact]
    public void Sync_NotAProxyPack_ChangesNothing()
    {
        var company = new SavedCompanyFile { Entries = [new SavedCompanyEntry { EntryIndex = 0, IsLieutenant = true, ExperiencePoints = 80 }] };

        Assert.False(LoneWolfAspects.Sync(company, season: null));
    }

    [Fact]
    public void ProgressOf_Aspect_SharesTheLoneWolfsSeason()
    {
        var company = ProxyPack(startingXp: 15);
        LoneWolfAspects.Sync(company, season: null);
        var season = new SeasonFile
        {
            Rounds = [new SeasonRound { RoundIndex = 1, MissionResults = new SeasonMissionResult { UnitResults = [new() { EntryIndex = 3, XpGained = 6 }] } }]
        };

        var aspect = SeasonTroopers.ProgressOf(company.Entries[1], season);

        Assert.Equal(15 + 6, aspect.TotalXp);
        Assert.Equal(3, aspect.EntryIndex);
    }

    [Fact]
    public void Renown_Aspect_AddsOnlyItsLoadout()
    {
        var company = ProxyPack(startingXp: 15);
        LoneWolfAspects.Sync(company, season: null);
        var season = new SeasonFile();
        season.UnitGear[4] = [new SeasonUnitGear { ItemName = "Pistol", Slot = GearSlot.Sidearm.Key() }];
        var catalog = InventoryCatalog.From([InventoryTestStores.NumberOne]);
        var servants = new HashSet<int>();

        var aspect = SeasonRenown.Of(company.Entries[1], company, season, catalog, servants);

        Assert.Equal(new(0, 3, 0), aspect);
        Assert.Equal(12 + 15 + 20 + 3, SeasonRenown.OfCompany(company, season, catalog, servants));
    }

    [Fact]
    public void CanRetire_Aspect_Never()
    {
        var company = ProxyPack(startingXp: 15);
        LoneWolfAspects.Sync(company, season: null);

        Assert.False(SeasonRetirement.CanRetire(company.Entries[1], null, new HashSet<int>()));
    }
}
