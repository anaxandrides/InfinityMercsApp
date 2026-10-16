using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.Season;

namespace InfinityMercsApp.UnitTests.Services.Season;

public sealed class SeasonRetirementTests
{
    // A captain, a doctor with 6 XP and its servant, and a rifleman.
    private static SavedCompanyFile Company() => new()
    {
        Entries =
        [
            new SavedCompanyEntry { EntryIndex = 1, Name = "Fusilier", IsLieutenant = true, Cost = 10 },
            new SavedCompanyEntry { EntryIndex = 2, Name = "Doctor", CustomName = " Doc ", Cost = 18, ExperiencePoints = 6 },
            new SavedCompanyEntry { EntryIndex = 3, Name = "Servant", IsPeripheralUnit = true, ParentEntryIndex = 2 },
            new SavedCompanyEntry { EntryIndex = 4, Name = "Rifleman", Cost = 12 }
        ]
    };

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(4, true)]
    public void AnyoneButTheCaptainAndPeripheralsCanRetire(int entryIndex, bool canRetire) =>
        Assert.Equal(canRetire, SeasonRetirement.For(Company(), entryIndex, new SeasonFile(), InventoryCatalog.Empty) is not null);

    [Fact]
    public void ATrooperRetiresWithItsCostAndXpAndItsPeripherals()
    {
        var trooper = SeasonRetirement.For(Company(), 2, new SeasonFile(), InventoryCatalog.Empty);

        Assert.NotNull(trooper);
        Assert.Equal(("Doc", 18 + 6), (trooper.Name, trooper.Renown));
        Assert.Equal([3], trooper.Peripherals.Select(peripheral => peripheral.EntryIndex));
    }

    [Fact]
    public void ARetiredTrooperCantRetireAgain()
    {
        var season = new SeasonFile { Retired = [new RetiredTrooper { EntryIndex = 4 }] };

        Assert.Null(SeasonRetirement.For(Company(), 4, season, InventoryCatalog.Empty));
    }

    [Fact]
    public void CompanyRenownSwapsARetiredTroopersRenownForWhatItKept()
    {
        var season = new SeasonFile { Retired = [new RetiredTrooper { EntryIndex = 4, Renown = 15 }] };

        Assert.Equal(10 + 18 + 6 + 15, SeasonRenown.OfCompany(Company(), season, InventoryCatalog.Empty));
    }
}
