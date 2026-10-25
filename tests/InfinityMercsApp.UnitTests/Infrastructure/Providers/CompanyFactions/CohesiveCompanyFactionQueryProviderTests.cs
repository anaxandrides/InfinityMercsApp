using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Providers.CompanyFactions;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Infrastructure.Providers.CompanyFactions;

/// <summary>A Cohesive company's units and filter names come merged from every sectorial it draws on.</summary>
public sealed class CohesiveCompanyFactionQueryProviderTests
{
    private readonly IFactionProvider _factions = Substitute.For<IFactionProvider>();
    private readonly CohesiveCompanyFactionQueryProvider _provider;

    public CohesiveCompanyFactionQueryProviderTests() => _provider = new CohesiveCompanyFactionQueryProvider(_factions);

    [Fact]
    public void GetFilterQuerySource_NoUsableFactions_IsEmpty()
    {
        var result = _provider.GetFilterQuerySource([0, -1]);

        Assert.Empty(result.MergedMercsListEntries);
        Assert.Empty(result.SkillsLookup);
    }

    [Fact]
    public void GetFilterQuerySource_Filters_MergesNamesFirstFactionFirst()
    {
        Snapshot(101, Section(ArmyFilterSection.Skills, new ArmyFilterItem(1, " Camo ", null, null)),
            Section(ArmyFilterSection.Type, new ArmyFilterItem(1, "LI", null, null)));
        Snapshot(102, Section(ArmyFilterSection.Skills, new ArmyFilterItem(1, "Other", null, null), new ArmyFilterItem(2, "Doctor", null, null)),
            Section(ArmyFilterSection.Weapons, new ArmyFilterItem(5, "Rifle", null, null)),
            Section(ArmyFilterSection.Equip, new ArmyFilterItem(6, "MediKit", null, null)),
            Section(ArmyFilterSection.Chars, new ArmyFilterItem(7, "Cube", null, null)),
            Section(ArmyFilterSection.Ammunition, new ArmyFilterItem(8, "AP", null, null), new ArmyFilterItem(9, " ", null, null)));

        var result = _provider.GetFilterQuerySource([101, 102, 103]);

        Assert.Equal("Camo", result.SkillsLookup[1]);
        Assert.Equal("Doctor", result.SkillsLookup[2]);
        Assert.Equal("LI", result.TypeLookup[1]);
        Assert.Equal("Rifle", result.WeaponsLookup[5]);
        Assert.Equal("MediKit", result.EquipmentLookup[6]);
        Assert.Equal("Cube", result.CharacteristicsLookup[7]);
        Assert.Equal(["AP"], result.AmmoLookup.Values);
    }

    [Fact]
    public void GetFilterQuerySource_SameUnitInTwoFactions_ListsItOnceFromBoth()
    {
        Units(101, Unit(101, 10, "Fusilier", Group(1, Profile(1, "Fusilier"), Option(1, "10"))));
        Units(102, Unit(102, 20, " fusilier ", Group(1, Profile(1, "Fusilier"), Option(1, "10"))));

        var entry = Assert.Single(_provider.GetFilterQuerySource([102, 101, 101]).MergedMercsListEntries);

        Assert.Equal([101, 102], entry.SourceFactionIds);
        var group = Assert.Single(entry.ProfileGroups.Groups);
        Assert.Single(group.Profiles);
        Assert.Single(group.Options);
    }

    [Fact]
    public void GetFilterQuerySource_SameUnitWithMoreLoadouts_GainsTheMissingOnes()
    {
        Units(101, Unit(101, 10, "Fusilier", Group(1, Profile(1, "Fusilier"), Option(1, "10"))));
        Units(102, Unit(102, 20, "Fusilier",
            Group(1, Profile(1, "Fusilier"), Option(1, "10"), Option(2, "12")),
            Group(2, Profile(2, "Remote"), Option(3, "5"))));

        var entry = Assert.Single(_provider.GetFilterQuerySource([101, 102]).MergedMercsListEntries);

        Assert.Equal([1, 2], entry.ProfileGroups.Groups.Select(group => group.Id));
        Assert.Equal([1, 2], entry.ProfileGroups.Groups[0].Options.Select(option => option.Id));
    }

    [Fact]
    public void GetFilterQuerySource_UnitMissingInOneFaction_KeepsTheOtherProfiles()
    {
        Units(101, Unit(101, 10, "Fusilier", Group(1, Profile(1, "Fusilier"), Option(1, "10"))));
        _factions.GetResumeByFactionMercsOnly(102).Returns([new Resume { FactionId = 102, UnitId = 99, Name = "Fusilier" }]);

        var entry = Assert.Single(_provider.GetFilterQuerySource([101, 102]).MergedMercsListEntries);

        Assert.Single(entry.ProfileGroups.Groups);
    }

    [Fact]
    public void GetFilterQuerySource_FirstFactionLacksTheUnit_TakesTheSecondFactionsProfiles()
    {
        _factions.GetResumeByFactionMercsOnly(101).Returns([new Resume { FactionId = 101, UnitId = 0, Name = "Fusilier" }]);
        Units(102, Unit(102, 20, "Fusilier", Group(1, Profile(1, "Fusilier"), Option(1, "10"))));

        var entry = Assert.Single(_provider.GetFilterQuerySource([101, 102]).MergedMercsListEntries);

        Assert.Single(entry.ProfileGroups.Groups);
    }

    [Fact]
    public void GetFilterQuerySource_UnnamedRow_IsLeftOut()
    {
        _factions.GetResumeByFactionMercsOnly(101).Returns([new Resume { FactionId = 101, UnitId = 1, Name = " " }]);

        Assert.Empty(_provider.GetFilterQuerySource([101]).MergedMercsListEntries);
    }

    [Fact]
    public void GetFilterQuerySource_Cancelled_Throws() =>
        Assert.Throws<OperationCanceledException>(() => _provider.GetFilterQuerySource([101], new CancellationToken(true)));

    private void Snapshot(int factionId, params IEnumerable<KeyValuePair<string, IReadOnlyList<ArmyFilterItem>>> sections) =>
        _factions.GetFactionSnapshot(factionId).Returns(new Faction
        {
            FactionId = factionId,
            Filters = new ArmyFilterCatalog(sections.ToDictionary(section => section.Key, section => section.Value))
        });

    private static KeyValuePair<string, IReadOnlyList<ArmyFilterItem>> Section(string name, params IEnumerable<ArmyFilterItem> items) =>
        new(name, items.ToList());

    private void Units(int factionId, Unit unit)
    {
        _factions.GetResumeByFactionMercsOnly(factionId).Returns([new Resume { FactionId = factionId, UnitId = unit.UnitId, Name = unit.Name, Type = 1 }]);
        _factions.GetUnit(factionId, unit.UnitId).Returns(unit);
    }

    private static Unit Unit(int factionId, int unitId, string name, params IEnumerable<ProfileGroup> groups) => new()
    {
        FactionId = factionId,
        UnitId = unitId,
        Name = name,
        ProfileGroups = new UnitProfileGroups(groups.ToList())
    };

    private static ProfileGroup Group(int id, UnitProfile profile, params IEnumerable<ProfileOption> options) => new()
    {
        Id = id,
        Profiles = [profile],
        Options = options.ToList()
    };

    private static UnitProfile Profile(int id, string name) => new() { Id = id, Name = name };

    private static ProfileOption Option(int id, string cost) => new() { Id = id, Name = $"Option {id}", Cost = cost };
}
