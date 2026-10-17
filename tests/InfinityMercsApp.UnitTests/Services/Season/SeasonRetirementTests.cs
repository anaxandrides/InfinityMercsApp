using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Domain.Season.Retirement;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.Season;

namespace InfinityMercsApp.UnitTests.Services.Season;

public sealed class SeasonRetirementTests
{
    // A captain, a doctor with 6 XP and its servant, a drummer and its drumbot, a peripheral of its own.
    private static SavedCompanyFile Company() => new()
    {
        Entries =
        [
            new SavedCompanyEntry { EntryIndex = 1, Name = "Fusilier", IsLieutenant = true, Cost = 10 },
            new SavedCompanyEntry { EntryIndex = 2, Name = "Doctor", CustomName = " Doc ", Cost = 18, ExperiencePoints = 6 },
            new SavedCompanyEntry { EntryIndex = 3, Name = "Servant", IsPeripheralUnit = true, ParentEntryIndex = 2 },
            new SavedCompanyEntry { EntryIndex = 4, Name = "Drummer", Cost = 12 },
            new SavedCompanyEntry { EntryIndex = 5, Name = "Drumbot", IsPeripheralUnit = true, ParentEntryIndex = 4 }
        ]
    };

    private static bool IsServant(SavedCompanyEntry entry) => entry.Name == "Servant";

    private static RetiringTrooper? Retiring(int entryIndex, SeasonFile? season = null) =>
        SeasonRetirement.For(Company(), entryIndex, season ?? new SeasonFile(), InventoryCatalog.Empty, IsServant);

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(4, true)]
    [InlineData(5, true)]
    public void AnyoneButTheCaptainAndServantPeripheralsCanRetire(int entryIndex, bool canRetire) =>
        Assert.Equal(canRetire, Retiring(entryIndex) is not null);

    // The drumbot is a trooper of its own, so it stays.
    [Fact]
    public void OnlyServantPeripheralsLeaveWithTheirTrooper()
    {
        var drummer = Retiring(4);

        Assert.NotNull(drummer);
        Assert.Empty(drummer.Peripherals);
    }

    [Fact]
    public void ATrooperRetiresWithItsCostAndXpAndItsPeripherals()
    {
        var trooper = Retiring(2);

        Assert.NotNull(trooper);
        Assert.Equal(("Doc", 18 + 6), (trooper.Name, trooper.Renown));
        Assert.Equal([3], trooper.Peripherals.Select(peripheral => peripheral.EntryIndex));
    }

    [Fact]
    public void ARetiredTrooperCantRetireAgain()
    {
        var season = new SeasonFile { Retired = [new RetiredTrooper { EntryIndex = 4 }] };

        Assert.Null(Retiring(4, season));
    }

    [Fact]
    public void CompanyRenownSwapsARetiredTroopersRenownForWhatItKept()
    {
        var season = new SeasonFile { Retired = [new RetiredTrooper { EntryIndex = 4, Renown = 15 }] };

        Assert.Equal(10 + 18 + 6 + 15, SeasonRenown.OfCompany(Company(), season, InventoryCatalog.Empty, IsServant));
    }
}
