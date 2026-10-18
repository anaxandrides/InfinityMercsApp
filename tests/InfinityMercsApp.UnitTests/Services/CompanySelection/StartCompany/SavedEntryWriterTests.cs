using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Company.Selection.Items;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.StartCompany;

public sealed class SavedEntryWriterTests
{
    private readonly SavedEntryWriter _writer = new(Army101Data.Create());

    private static MercsCompanyEntry Trooper(string name, int cost, MercsCompanyEntry? servantOwner = null, bool hasPeripheral = false) => new()
    {
        Name = name,
        BaseUnitName = name,
        CostValue = cost,
        SourceFactionId = Army101Data.PanOceania,
        CanOwnServant = servantOwner is null,
        ServantOwner = servantOwner,
        HasPeripheralStatBlock = hasPeripheral,
        PeripheralNameHeading = hasPeripheral ? "Drumbot" : string.Empty
    };

    [Fact]
    public void Build_ServantAfterItsOwner_SavesItAsTheOwnersPeripheralAtItsOwnCost()
    {
        var kaplan = Trooper("Kaplan", 24);

        var entries = _writer.Build([kaplan, Trooper("Palbot", 3, kaplan)], "Captain", new SavedImprovedCaptainStats());

        Assert.False(entries[0].IsPeripheralUnit);
        Assert.True(entries[1] is { IsPeripheralUnit: true, ParentEntryIndex: 0, Cost: 3 });
    }

    [Fact]
    public void Build_Lieutenant_CarriesTheCaptainUpgrades()
    {
        var captain = new MercsCompanyEntry { Name = "Kaplan", IsLieutenant = true, SourceFactionId = Army101Data.PanOceania };

        var entries = _writer.Build([captain, Trooper("Fusilier", 10)], "Captain", new SavedImprovedCaptainStats());

        Assert.Equal([true, false], entries.Select(entry => entry.CarriesCaptainUpgrades));
    }

    [Fact]
    public void Build_ServantBeforeItsOwner_PointsAtWhereTheOwnerIsSaved()
    {
        var drummer = Trooper("Drummer", 20, hasPeripheral: true);

        var entries = _writer.Build([Trooper("Fusilier", 10), Trooper("Palbot", 3, drummer), drummer], "Captain", new SavedImprovedCaptainStats(), firstEntryIndex: 5);

        Assert.Equal([5, 6, 7, 8], entries.Select(entry => entry.EntryIndex));
        Assert.Equal(7, entries[1].ParentEntryIndex);
        Assert.Equal("Drummer", entries[2].Name);
    }

    [Fact]
    public void RecruitEntries_ServantOfATrooperOnTheRoster_PointsAtItsSavedEntry()
    {
        var onRoster = new MercsCompanyEntry { Name = "Kaplan", IsOnRoster = true, SavedEntryIndex = 3, CanOwnServant = true };

        var entry = Assert.Single(_writer.RecruitEntries([Trooper("Palbot", 3, onRoster)], firstEntryIndex: 9));

        Assert.True(entry is { EntryIndex: 9, IsPeripheralUnit: true, ParentEntryIndex: 3, Cost: 3 });
    }
}
