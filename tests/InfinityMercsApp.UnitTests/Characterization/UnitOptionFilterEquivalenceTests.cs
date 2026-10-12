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
    public static IEnumerable<object[]> CorpusSources =>
    [
        ["army-101.json"],
        ["infinitymercs.db3"]
    ];

    private sealed record FilterSpec(
        string Description,
        IReadOnlyList<(UnitFilterField Field, string[] Values, UnitFilterMatchMode Mode)> Terms,
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

            foreach (var (requireLieutenant, requireZeroSwc, maxCost, includeProfiles) in new[]
                     {
                         (false, false, (int?)null, true),
                         (true, true, (int?)25, false),
                         (false, true, (int?)40, true)
                     })
            {
                var legacySets = NewSets();
                LegacyFilter.AddFilterOptionsFromVisibleProfilesAndOptions(
                    unit.ProfileGroupsJson, lookups.Chars, lookups.Skills, lookups.Equip, lookups.Weapons, lookups.Ammo,
                    requireLieutenant, requireZeroSwc, maxCost, includeProfiles,
                    legacySets[0], legacySets[1], legacySets[2], legacySets[3], legacySets[4]);
                var sets = NewSets();
                UnitOptionFilter.CollectFilterOptions(
                    groups, filterLookups, requireLieutenant, requireZeroSwc, maxCost, includeProfiles,
                    new UnitFilterOptionSets(sets[0], sets[1], sets[2], sets[3], sets[4]));

                for (var i = 0; i < sets.Length; i++)
                {
                    if (!legacySets[i].SetEquals(sets[i]))
                    {
                        mismatches.Add($"{unit.Key} filter-options[{i}] lt={requireLieutenant} swc0={requireZeroSwc} max={maxCost}");
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

    private static HashSet<string>[] NewSets() =>
        Enumerable.Range(0, 5).Select(_ => new HashSet<string>(StringComparer.OrdinalIgnoreCase)).ToArray();

    /// <summary>Filter specs built from names this unit (or its army) actually uses, so terms really match.</summary>
    private static IEnumerable<FilterSpec> SpecsFor(CorpusUnit unit, CorpusLookups lookups)
    {
        var sets = NewSets();
        LegacyFilter.AddFilterOptionsFromVisibleProfilesAndOptions(
            unit.ProfileGroupsJson, lookups.Chars, lookups.Skills, lookups.Equip, lookups.Weapons, lookups.Ammo,
            false, false, null, true, sets[0], sets[1], sets[2], sets[3], sets[4]);
        var chars = sets[0].OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var skills = sets[1].OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var equipment = sets[2].OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var weapons = sets[3].OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var ammo = sets[4].OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var otherWeapon = lookups.Weapons.Values.FirstOrDefault(name => !weapons.Contains(name)) ?? "Not A Weapon";

        yield return new FilterSpec("none", [], null, null, false, false, null);
        yield return new FilterSpec("lieutenant", [], null, null, true, false, null);
        yield return new FilterSpec("zero-swc+cap", [], null, null, false, true, 30);
        yield return new FilterSpec("points 20-40", [], 20, 40, false, false, null);
        yield return new FilterSpec("weapon any", [(UnitFilterField.Weapons, weapons.Take(1).Append(otherWeapon).ToArray(), UnitFilterMatchMode.Any)], null, null, false, false, null);
        yield return new FilterSpec("weapons all", [(UnitFilterField.Weapons, weapons.Take(2).ToArray(), UnitFilterMatchMode.All)], null, null, false, false, null);
        yield return new FilterSpec("weapon missing", [(UnitFilterField.Weapons, [otherWeapon], UnitFilterMatchMode.Any)], null, null, false, false, null);
        yield return new FilterSpec("skills all", [(UnitFilterField.Skills, skills.Take(3).ToArray(), UnitFilterMatchMode.All)], null, null, false, false, null);
        yield return new FilterSpec("chars+equip", [
            (UnitFilterField.Characteristics, chars.Take(1).ToArray(), UnitFilterMatchMode.Any),
            (UnitFilterField.Equipment, equipment.Take(1).ToArray(), UnitFilterMatchMode.Any)], null, null, false, true, null);
        yield return new FilterSpec("ammo", [(UnitFilterField.Ammo, ammo.Take(2).ToArray(), UnitFilterMatchMode.Any)], null, 35, false, false, null);
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
