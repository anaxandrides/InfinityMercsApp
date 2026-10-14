using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Domain.Models.Filtering;
using InfinityMercsApp.Infrastructure.Json;
using LegacyCriteria = InfinityMercsApp.UnitTests.Legacy.UnitFilterCriteria;
using LegacyField = InfinityMercsApp.UnitTests.Legacy.UnitFilterField;
using LegacyFilter = InfinityMercsApp.UnitTests.Legacy.LegacyCompanyUnitFilterService;
using LegacyMode = InfinityMercsApp.UnitTests.Legacy.UnitFilterMatchMode;
using LegacyTerm = InfinityMercsApp.UnitTests.Legacy.UnitFilterTerm;

namespace InfinityMercsApp.UnitTests.Characterization;

/// <summary>
/// Proves <see cref="UnitOptionFilter"/> (domain models) answers exactly like the legacy
/// CompanyUnitFilterService (raw JSON) for every real unit.
/// </summary>
public sealed class UnitOptionFilterEquivalenceTests
{
    public static TheoryData<string> CorpusSources => new() { "army-101.json", "infinitymercs.db3" };

    private sealed record FilterSpec(
        string Description,
        IReadOnlyList<(UnitFilterField Field, IReadOnlyList<string> Values, UnitFilterMatchMode Mode)> Terms,
        int? MinPoints,
        int? MaxPoints,
        bool RequireLieutenant,
        bool RequireZeroSwc,
        int? MaxCost);

    [Theory]
    [MemberData(nameof(CorpusSources))]
    public void VisibilityAndFilterOptions_MatchLegacyFilterService(string source)
    {
        var reader = new ArmyJsonReader();
        var mismatches = new List<string>();

        foreach (var unit in ReferenceCorpus.All.Where(x => x.Source == source))
        {
            var lookups = CorpusLookups.For(unit.ArmyFiltersJson);
            var filterLookups = new UnitFilterLookups(lookups.Chars, lookups.Skills, lookups.Equip, lookups.Weapons, lookups.Ammo);
            var groups = reader.ReadProfileGroups(unit.ProfileGroupsJson);

            IEnumerable<(bool RequireLieutenant, bool RequireZeroSwc, int? MaxCost, bool IncludeProfiles)> variants =
            [
                (false, false, null, true),
                (true, true, 25, false),
                (false, true, 40, true)
            ];
            foreach (var (requireLieutenant, requireZeroSwc, maxCost, includeProfiles) in variants)
            {
                var legacySets = CollectLegacy(unit, lookups, requireLieutenant, requireZeroSwc, maxCost, includeProfiles);
                var sets = new OptionNameSets();
                UnitOptionFilter.CollectFilterOptions(
                    groups, filterLookups, requireLieutenant, requireZeroSwc, maxCost, includeProfiles, sets.ToDomain());

                foreach (var (name, legacy) in legacySets.ByName)
                {
                    if (!legacy.SetEquals(sets.ByName[name]))
                    {
                        mismatches.Add($"{unit.Key} filter-options {name} lt={requireLieutenant} swc0={requireZeroSwc} max={maxCost}");
                    }
                }
            }

            foreach (var spec in SpecsFor(unit, lookups))
            {
                var expected = LegacyFilter.UnitHasVisibleOptionWithFilter(
                    unit.ProfileGroupsJson, lookups.Skills, lookups.Chars, lookups.Equip, lookups.Weapons, lookups.Ammo,
                    ToLegacy(spec), spec.RequireLieutenant, spec.RequireZeroSwc, spec.MaxCost,
                    name => name is null || !name.Contains('(', StringComparison.Ordinal));
                var actual = UnitOptionFilter.HasVisibleOption(
                    groups, filterLookups, ToDomain(spec), spec.RequireLieutenant, spec.RequireZeroSwc, spec.MaxCost,
                    name => name is null || !name.Contains('(', StringComparison.Ordinal));
                if (expected != actual)
                {
                    mismatches.Add($"{unit.Key} visible[{spec.Description}]: legacy={expected} domain={actual}");
                }
            }

            foreach (var name in PeripheralNameCandidates(groups))
            {
                var expectedOk = LegacyFilter.TryGetPeripheralUnitCost(
                    System.Text.Json.JsonDocument.Parse(unit.ProfileGroupsJson).RootElement, name, out var expectedCost);
                var actualOk = UnitOptionFilter.TryGetPeripheralUnitCost(groups, name, out var actualCost);
                if (expectedOk != actualOk || expectedCost != actualCost)
                {
                    mismatches.Add($"{unit.Key} peripheral-cost['{name}']: legacy=({expectedOk},{expectedCost}) domain=({actualOk},{actualCost})");
                }
            }

            if (mismatches.Count > 20)
            {
                break;
            }
        }

        Assert.True(mismatches.Count == 0, string.Join(Environment.NewLine, mismatches));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(99, false)]
    public void Classification_MatchesLegacyFilterService(int? unitType, bool expected)
    {
        var typeLookup = new Dictionary<int, string> { [1] = "LI", [2] = "MI" };
        var legacy = LegacyFilter.MatchesClassificationFilter(
            new LegacyCriteria { Terms = [new LegacyTerm(LegacyField.Classification, ["li"])] },
            unitType,
            typeLookup);
        var domain = UnitOptionFilter.MatchesClassification(
            new UnitFilterCriteria { Terms = [new UnitFilterTerm(UnitFilterField.Classification, ["li"])] },
            unitType,
            typeLookup);

        Assert.Equal(expected, legacy);
        Assert.Equal(legacy, domain);
        Assert.True(UnitOptionFilter.MatchesClassification(UnitFilterCriteria.None, unitType, typeLookup));
    }

    private static OptionNameSets CollectLegacy(
        CorpusUnit unit, CorpusLookups lookups, bool requireLieutenant, bool requireZeroSwc, int? maxCost, bool includeProfiles)
    {
        var sets = new OptionNameSets();
        LegacyFilter.AddFilterOptionsFromVisibleProfilesAndOptions(
            unit.ProfileGroupsJson, lookups.Chars, lookups.Skills, lookups.Equip, lookups.Weapons, lookups.Ammo,
            requireLieutenant, requireZeroSwc, maxCost, includeProfiles,
            sets.Characteristics, sets.Skills, sets.Equipment, sets.Weapons, sets.Ammo);
        return sets;
    }

    /// <summary>Filter specs built from names this unit (or its army) actually uses, so terms really match.</summary>
    private static IEnumerable<FilterSpec> SpecsFor(CorpusUnit unit, CorpusLookups lookups)
    {
        var sets = CollectLegacy(unit, lookups, requireLieutenant: false, requireZeroSwc: false, maxCost: null, includeProfiles: true);
        var chars = sets.Characteristics.OrderBy(x => x, StringComparer.Ordinal).ToList();
        var skills = sets.Skills.OrderBy(x => x, StringComparer.Ordinal).ToList();
        var equipment = sets.Equipment.OrderBy(x => x, StringComparer.Ordinal).ToList();
        var weapons = sets.Weapons.OrderBy(x => x, StringComparer.Ordinal).ToList();
        var ammo = sets.Ammo.OrderBy(x => x, StringComparer.Ordinal).ToList();
        var otherWeapon = lookups.Weapons.Values.FirstOrDefault(name => !weapons.Contains(name)) ?? "Not A Weapon";

        yield return new FilterSpec("none", [], null, null, false, false, null);
        yield return new FilterSpec("lieutenant", [], null, null, true, false, null);
        yield return new FilterSpec("zero-swc+cap", [], null, null, false, true, 30);
        yield return new FilterSpec("points 20-40", [], 20, 40, false, false, null);
        yield return new FilterSpec("weapon any", [(UnitFilterField.Weapons, weapons.Take(1).Append(otherWeapon).ToList(), UnitFilterMatchMode.Any)], null, null, false, false, null);
        yield return new FilterSpec("weapons all", [(UnitFilterField.Weapons, weapons.Take(2).ToList(), UnitFilterMatchMode.All)], null, null, false, false, null);
        yield return new FilterSpec("weapon missing", [(UnitFilterField.Weapons, [otherWeapon], UnitFilterMatchMode.Any)], null, null, false, false, null);
        yield return new FilterSpec("skills all", [(UnitFilterField.Skills, skills.Take(3).ToList(), UnitFilterMatchMode.All)], null, null, false, false, null);
        yield return new FilterSpec("chars+equip", [
            (UnitFilterField.Characteristics, chars.Take(1).ToList(), UnitFilterMatchMode.Any),
            (UnitFilterField.Equipment, equipment.Take(1).ToList(), UnitFilterMatchMode.Any)], null, null, false, true, null);
        yield return new FilterSpec("ammo", [(UnitFilterField.Ammo, ammo.Take(2).ToList(), UnitFilterMatchMode.Any)], null, 35, false, false, null);
        yield return new FilterSpec("classification ignored", [(UnitFilterField.Classification, ["Anything"], UnitFilterMatchMode.Any)], null, null, false, false, null);
    }

    private static IEnumerable<string> PeripheralNameCandidates(UnitProfileGroups groups)
    {
        foreach (var group in groups.Groups)
        {
            if (!string.IsNullOrWhiteSpace(group.Isc))
            {
                yield return group.Isc;
            }

            foreach (var profile in group.Profiles.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
            {
                yield return profile.Name!.ToLowerInvariant() + " (2)";
            }
        }

        yield return "No Such Peripheral";
        yield return "  ";
    }

    private static LegacyCriteria ToLegacy(FilterSpec spec) => new()
    {
        Terms = spec.Terms.Select(t => new LegacyTerm((LegacyField)(int)t.Field, t.Values, (LegacyMode)(int)t.Mode)).ToList(),
        MinPoints = spec.MinPoints,
        MaxPoints = spec.MaxPoints
    };

    private static UnitFilterCriteria ToDomain(FilterSpec spec) => new()
    {
        Terms = spec.Terms.Select(t => new UnitFilterTerm(t.Field, t.Values, t.Mode)).ToList(),
        MinPoints = spec.MinPoints,
        MaxPoints = spec.MaxPoints
    };
}
