using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.ViewModels.Company.Selection.Workflows;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection.Workflows;

public sealed class RosterEntriesTests
{
    private static SavedCompanyFile Company() => new()
    {
        Entries =
        [
            new SavedCompanyEntry { EntryIndex = 0, Name = "Fusilier", BaseUnitName = "Fusilier", SourceFactionId = 101, SourceUnitId = 5, Cost = 10, ExperiencePoints = 4 },
            new SavedCompanyEntry { EntryIndex = 1, Name = "Trooper", CustomName = " Kestrel ", IsLieutenant = true, SourceFactionId = 101, SourceUnitId = 7, Cost = 20, CurrentBs = "13" },
            new SavedCompanyEntry { EntryIndex = 2, Name = "Traktor Mul", IsPeripheralUnit = true, SourceFactionId = 101, SourceUnitId = 7 }
        ]
    };

    [Fact]
    public void TheTroopersGoOnTheRosterLieutenantFirstWithoutPeripherals()
    {
        var entries = RosterEntries.From(Company());

        Assert.Equal(["Kestrel", "Fusilier"], entries.Select(entry => entry.Name));
        Assert.All(entries, entry => Assert.True(entry.IsOnRoster));
        Assert.All(entries, entry => Assert.False(entry.CanRemove));
        Assert.True(entries[0].IsLieutenant);
    }

    [Fact]
    public void EachKeepsWhatAvaAndThePointsNeed()
    {
        var kestrel = RosterEntries.From(Company())[0];

        Assert.Equal((101, 7, 20, "C 20"), (kestrel.SourceFactionId, kestrel.SourceUnitId, kestrel.CostValue, kestrel.CostDisplay));
        Assert.Equal("SVGCache/units/101-7.svg", kestrel.PackagedLogoPath);
        Assert.Contains("BS 13", kestrel.Subtitle);
    }
}
