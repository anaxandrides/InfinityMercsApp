using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Services.Season.Peripherals;
using InfinityMercsApp.ViewModels.Company.Selection.Items;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection.Items;

public sealed class RosterEntriesTests
{
    private static SavedCompanyFile Company() => new()
    {
        Entries =
        [
            new SavedCompanyEntry { EntryIndex = 0, Name = "Fusilier", BaseUnitName = "Fusilier", SourceFactionId = 101, SourceUnitId = 5, Cost = 10, ExperiencePoints = 4 },
            new SavedCompanyEntry { EntryIndex = 1, Name = "Trooper", CustomName = " Kestrel ", IsLieutenant = true, CarriesCaptainUpgrades = true, SourceFactionId = 101, SourceUnitId = 7, Cost = 20, CurrentBs = "13" },
            new SavedCompanyEntry { EntryIndex = 2, Name = "Traktor Mul", IsPeripheralUnit = true, SourceFactionId = 101, SourceUnitId = 7 }
        ]
    };

    [Fact]
    public void From_Company_PutsTroopersOnTheRosterLieutenantFirst()
    {
        var entries = RosterEntries.From(Company(), [], CompanyServants.None);

        Assert.Equal(["Kestrel", "Fusilier"], entries.Select(entry => entry.Name));
        Assert.All(entries, entry => Assert.True(entry.IsOnRoster));
        Assert.All(entries, entry => Assert.False(entry.CanRemove));
        Assert.True(entries[0].IsLieutenant);
    }

    [Fact]
    public void From_Entry_KeepsWhatAvaAndThePointsNeed()
    {
        var kestrel = RosterEntries.From(Company(), [], CompanyServants.None)[0];

        Assert.Equal((101, 7, 20, "C 20"), (kestrel.SourceFactionId, kestrel.SourceUnitId, kestrel.CostValue, kestrel.CostDisplay));
        Assert.Equal("SVGCache/units/101-7.svg", kestrel.PackagedLogoPath);
        Assert.Contains("BS 13", kestrel.Subtitle);
    }

    [Fact]
    public void From_RetiredTrooper_StaysNamedAsRetired()
    {
        var entries = RosterEntries.From(Company(), [0], CompanyServants.None);

        Assert.Equal(["Kestrel", "Fusilier (Retired)"], entries.Select(entry => entry.Name));
        Assert.Equal(5, entries[1].SourceUnitId);
    }

    [Fact]
    public void From_RecruitedServant_FollowsTheDoctorItBelongsTo()
    {
        var company = Company();
        company.Entries.Add(new SavedCompanyEntry { EntryIndex = 3, Name = "Palbot", IsPeripheralUnit = true, ParentEntryIndex = 0, SourceFactionId = 101, SourceUnitId = 9, Cost = 3 });
        var servants = new CompanyServants(new HashSet<int> { 2, 3 }, new HashSet<int> { 0 });

        var entries = RosterEntries.From(company, [], servants);

        Assert.Equal(["Kestrel", "Fusilier", "Palbot"], entries.Select(entry => entry.Name));
        Assert.Same(entries[1], entries[2].ServantOwner);
        Assert.Equal("Servant of Fusilier", entries[2].ServantOfText);
    }

    [Fact]
    public void From_DoctorOrEngineer_CanOwnAServantAndKeepsItsSavedIndex()
    {
        var entries = RosterEntries.From(Company(), [], new CompanyServants(new HashSet<int>(), new HashSet<int> { 0 }));

        Assert.False(entries[0].CanOwnServant);
        Assert.True(entries[1].CanOwnServant);
        Assert.Equal(0, entries[1].SavedEntryIndex);
    }
}
