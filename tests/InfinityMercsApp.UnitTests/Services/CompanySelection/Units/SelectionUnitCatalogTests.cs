using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Services.CompanySelection.Units;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.Units;

public sealed class SelectionUnitCatalogTests
{
    private static readonly Dictionary<int, string> Types = new() { [1] = "LI" };
    private static readonly Dictionary<int, string> Categories = new() { [10] = "Garrison Troops" };

    private static FactionUnits Faction(int id, IReadOnlyList<Resume> resumes, IReadOnlyList<Resume>? specOps = null) =>
        new(id, resumes, specOps ?? [], Types, Categories, FireteamChart: null);

    private static Resume Resume(int unitId, string name) =>
        new() { UnitId = unitId, Name = name, Slug = name.ToLowerInvariant(), Type = 1, Category = 10 };

    [Fact]
    public void Merge_SameNameInTwoFactions_ListsItOnceFromTheFirst()
    {
        var merged = SelectionUnitCatalog.Merge([Faction(101, [Resume(5, "Fusilier")]), Faction(102, [Resume(5, " Fusilier ")])]);

        var unit = Assert.Single(merged.Units);
        Assert.Equal(101, unit.FactionId);
        Assert.Equal("LI - Garrison Troops", unit.Subtitle);
        Assert.False(unit.IsSpecOps);
    }

    [Fact]
    public void Merge_SpecOps_FollowTheUnitsMarkedAsSpecOps()
    {
        var merged = SelectionUnitCatalog.Merge([Faction(101, [Resume(5, "Fusilier")], [Resume(1912, "INDIGO SPEC-OPS")])]);

        Assert.Equal(["Fusilier", "INDIGO SPEC-OPS"], merged.Units.Select(x => x.Name));
        Assert.Equal("LI - Garrison Troops", merged.Units[1].Subtitle);
        Assert.False(merged.Units[0].IsSpecOps);
        Assert.True(merged.Units[1].IsSpecOps);
    }

    [Fact]
    public void Merge_SameSpecOpsInBothSectorials_ListsItOnce()
    {
        var merged = SelectionUnitCatalog.Merge(
            [Faction(101, [], [Resume(1912, "INDIGO SPEC-OPS")]), Faction(102, [], [Resume(1912, "INDIGO SPEC-OPS")])]);

        Assert.Equal(101, Assert.Single(merged.Units).FactionId);
    }
}
