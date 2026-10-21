using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Army.SpecOps;
using InfinityMercsApp.Services.CompanySelection.Units;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.Units;

public sealed class SelectionUnitCatalogTests
{
    private static readonly Dictionary<int, string> Types = new() { [1] = "LI" };
    private static readonly Dictionary<int, string> Categories = new() { [10] = "Garrison Troops" };

    private static FactionUnits Faction(int id, IReadOnlyList<Resume> resumes, IReadOnlyList<SpecopsUnit>? specOps = null) =>
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
    public void Merge_SpecOpsUnits_FollowNamedAfterTheirUnit()
    {
        var merged = SelectionUnitCatalog.Merge([Faction(101, [Resume(5, "Fusilier")], [new SpecopsUnit { UnitId = 5 }, new SpecopsUnit { UnitId = 9 }])]);

        Assert.Equal(["Fusilier", "Fusilier - Spec Ops", "Unit 9 - Spec Ops"], merged.Units.Select(x => x.Name));
        Assert.Equal("LI - Garrison Troops", merged.Units[1].Subtitle);
        Assert.Equal("Spec Ops", merged.Units[2].Subtitle);
        Assert.All(merged.Units.Skip(1), unit => Assert.True(unit.IsSpecOps));
    }
}
