using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Filtering;

namespace InfinityMercsApp.UnitTests.Filtering;

public sealed class UnitFilterLookupsTests
{
    private static ArmyFilterCatalog Catalog(string section, params IEnumerable<(int Id, string Name)> items) =>
        new(new Dictionary<string, IReadOnlyList<ArmyFilterItem>>
        {
            [section] = items.Select(item => new ArmyFilterItem(item.Id, item.Name, null, null)).ToList()
        });

    [Fact]
    public void From_Catalog_ReadsEachLookupFromItsSection()
    {
        var lookups = UnitFilterLookups.From(Catalog(ArmyFilterSection.Ammunition, (2, "AP")));

        Assert.Equal("AP", lookups.Ammo[2]);
        Assert.Empty(lookups.Weapons);
    }

    [Fact]
    public void From_NoFilters_ReturnsEmptyLookups() => Assert.Empty(UnitFilterLookups.From(null).Skills);

    [Fact]
    public void Merge_SeveralCatalogs_KeepsFirstNonBlankNamePerId()
    {
        var merged = UnitFilterLookups.Merge(
        [
            Catalog(ArmyFilterSection.Skills, (1, " Camouflage "), (2, " ")),
            Catalog(ArmyFilterSection.Skills, (1, "Mimetism"), (2, "Stealth"), (3, "Climbing Plus"))
        ]);

        Assert.Equal(new Dictionary<int, string> { [1] = "Camouflage", [2] = "Stealth", [3] = "Climbing Plus" }, merged.Skills);
    }

    [Fact]
    public void Sorted_IgnoringCaseSets_DedupeAndSortAlphabetically()
    {
        var sets = UnitFilterOptionSets.IgnoringCase();
        sets.Weapons.Add("spitfire");
        sets.Weapons.Add("Spitfire");
        sets.Weapons.Add("Combi Rifle");

        Assert.Equal(["Combi Rifle", "spitfire"], UnitFilterOptionSets.Sorted(sets.Weapons));
    }
}
