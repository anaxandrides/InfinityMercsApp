using System.Text.Json;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.UnitTests.Legacy;

namespace InfinityMercsApp.UnitTests.Characterization;

/// <summary>
/// Proves the domain versions of the unit viewer's own display rules (API-ordered names, counted
/// peripherals, detail-tagged option names, filter-popup names) match the frozen ViewerViewModel copy.
/// </summary>
public sealed class ViewerRulesEquivalenceTests
{
    private static readonly (string Property, ProfileEntryKind Kind)[] Kinds =
    [
        ("chars", ProfileEntryKind.Chars),
        ("skills", ProfileEntryKind.Skills),
        ("equip", ProfileEntryKind.Equip),
        ("weapons", ProfileEntryKind.Weapons),
        ("peripheral", ProfileEntryKind.Peripheral)
    ];

    public static IEnumerable<object[]> CorpusSources =>
    [
        ["army-101.json", false],
        ["army-101.json", true],
        ["infinitymercs.db3", false],
        ["infinitymercs.db3", true]
    ];

    [Theory]
    [InlineData("+10")]
    [InlineData("+2.5")]
    [InlineData("+20")]
    [InlineData("+5")]
    [InlineData("+7.5")]
    [InlineData("20")]
    [InlineData("7.5")]
    public void RealDistanceExtras_FormatTheSameInViewerAndShared(string distance)
    {
        // Every DISTANCE extra in the bundled data; the viewer's own formatter and DistanceText agree on all.
        foreach (var inches in new[] { false, true })
        {
            Assert.Equal(
                LegacyViewerOptionRules.FormatExtraDisplay(new LegacyViewerOptionRules.ExtraDefinition(distance, "DISTANCE", null), inches),
                DistanceText.Convert(distance, inches));
        }
    }

    [Theory]
    [MemberData(nameof(CorpusSources))]
    public void ViewerDisplayRules_MatchLegacyViewer(string source, bool inches)
    {
        var reader = new ArmyJsonReader();
        var mismatches = new List<string>();

        foreach (var unit in ReferenceCorpus.All.Where(x => x.Source == source))
        {
            var lookups = CorpusLookups.For(unit.ArmyFiltersJson);
            var names = new ProfileEntryNames(reader.ReadArmyFilters(unit.ArmyFiltersJson).Extras, inches);
            var groups = reader.ReadProfileGroups(unit.ProfileGroupsJson);
            using var doc = JsonDocument.Parse(unit.ProfileGroupsJson);
            var root = doc.RootElement;

            var legacyGroups = root.EnumerateArray().ToList();
            for (var g = 0; g < legacyGroups.Count; g++)
            {
                var legacyGroup = legacyGroups[g];
                var group = groups.Groups[g];
                var legacyOptions = legacyGroup.GetProperty("options").EnumerateArray().ToList();
                for (var o = 0; o < legacyOptions.Count; o++)
                {
                    var legacyOption = legacyOptions[o];
                    var option = group.Options[o];
                    var where = $"g{g}o{o}";

                    foreach (var (property, kind) in Kinds)
                    {
                        var lookup = LegacyProfileSnapshot.LookupFor(property, lookups);
                        Compare(mismatches, unit, $"{where} in-api-order[{property}]",
                            LegacyViewerOptionRules.GetOrderedIdDisplayNamesFromEntries(
                                LegacyViewerOptionRules.GetOptionEntriesWithIncludes(root, legacyOption, property), lookup, lookups.Extras, inches),
                            names.InApiOrder(groups.GetEntriesWithIncludes(option, kind), lookup));
                        Compare(mismatches, unit, $"{where} plain[{property}]",
                            LegacyViewerOptionRules.GetOrderedIdNames(legacyOption, property, lookup),
                            ProfileEntryNames.PlainNamesInApiOrder(option.Entries(kind), lookup));
                    }

                    Compare(mismatches, unit, $"{where} counted-peripherals",
                        LegacyViewerOptionRules.GetCountedIdDisplayNamesFromEntries(
                            LegacyViewerOptionRules.GetDisplayPeripheralEntriesForOption(root, legacyGroup, legacyOption), lookups.Peripheral, lookups.Extras, inches),
                        names.CountedWithIds(groups.GetDisplayPeripheralEntries(group, option), lookups.Peripheral));

                    var baseName = option.Name ?? group.Isc ?? string.Empty;
                    Compare(mismatches, unit, $"{where} display-name",
                        LegacyViewerOptionRules.BuildOptionDisplayName(legacyOption, baseName, lookups.Equip, lookups.Skills),
                        ViewerOptionNames.WithDetailTags(option, baseName, lookups.Equip, lookups.Skills));
                }

                var legacyProfiles = legacyGroup.GetProperty("profiles").EnumerateArray().ToList();
                for (var p = 0; p < legacyProfiles.Count; p++)
                {
                    foreach (var (property, kind) in Kinds)
                    {
                        var lookup = LegacyProfileSnapshot.LookupFor(property, lookups);
                        Compare(mismatches, unit, $"g{g}p{p} profile-in-api-order[{property}]",
                            LegacyViewerOptionRules.GetOrderedIdDisplayNames(legacyProfiles[p], property, lookup, lookups.Extras, inches),
                            names.InApiOrder(group.Profiles[p].Entries(kind), lookup));
                    }
                }
            }

            var legacySets = Enumerable.Range(0, 5).Select(_ => new HashSet<string>(StringComparer.OrdinalIgnoreCase)).ToArray();
            var legacyMax = 0;
            LegacyViewerOptionRules.AddFilterOptionsFromProfilesAndOptions(
                unit.ProfileGroupsJson, lookups.Chars, lookups.Skills, lookups.Equip, lookups.Weapons, lookups.Ammo,
                legacySets[0], legacySets[1], legacySets[2], legacySets[3], legacySets[4], ref legacyMax);
            var sets = Enumerable.Range(0, 5).Select(_ => new HashSet<string>(StringComparer.OrdinalIgnoreCase)).ToArray();
            var max = ViewerOptionNames.CollectAllOptionNames(
                groups, lookups.Chars, lookups.Skills, lookups.Equip, lookups.Weapons, lookups.Ammo,
                sets[0], sets[1], sets[2], sets[3], sets[4]);
            Compare(mismatches, unit, "filter-popup max points", legacyMax.ToString(), max.ToString());
            for (var i = 0; i < sets.Length; i++)
            {
                Compare(mismatches, unit, $"filter-popup names[{i}]",
                    string.Join("|", legacySets[i].OrderBy(x => x, StringComparer.Ordinal)),
                    string.Join("|", sets[i].OrderBy(x => x, StringComparer.Ordinal)));
            }

            if (mismatches.Count > 20)
            {
                break;
            }
        }

        Assert.True(mismatches.Count == 0, string.Join(Environment.NewLine, mismatches));
    }

    private static void Compare(List<string> mismatches, CorpusUnit unit, string what, List<(int Id, string Name)> legacy, List<(int Id, string Name)> actual)
    {
        Compare(mismatches, unit, what,
            string.Join(" | ", legacy.Select(x => $"{x.Id}:{x.Name}")),
            string.Join(" | ", actual.Select(x => $"{x.Id}:{x.Name}")));
    }

    private static void Compare(List<string> mismatches, CorpusUnit unit, string what, string legacy, string actual)
    {
        if (legacy != actual)
        {
            mismatches.Add($"{unit.Key} {what}: legacy='{legacy}' domain='{actual}'");
        }
    }
}
