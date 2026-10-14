using System.Text.Json;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.UnitTests.Legacy;

namespace InfinityMercsApp.UnitTests.Characterization;

/// <summary>
/// Proves the domain versions of the unit-detail rules (entry display names, traits, statline values,
/// peripheral lookups) match the frozen legacy JSON implementations on every real unit.
/// </summary>
public sealed class UnitDetailRulesEquivalenceTests
{
    private static readonly IReadOnlyList<(string Property, ProfileEntryKind Kind)> Kinds =
    [
        ("chars", ProfileEntryKind.Chars),
        ("skills", ProfileEntryKind.Skills),
        ("equip", ProfileEntryKind.Equip),
        ("weapons", ProfileEntryKind.Weapons),
        ("peripheral", ProfileEntryKind.Peripheral)
    ];

    public static TheoryData<string, bool> CorpusSources => new()
    {
        { "army-101.json", false },
        { "army-101.json", true },
        { "infinitymercs.db3", false },
        { "infinitymercs.db3", true }
    };

    [Theory]
    [InlineData("+5", false)]
    [InlineData("+5", true)]
    [InlineData("10cm", true)]
    [InlineData("4\"", false)]
    [InlineData("-2.5 inches", false)]
    [InlineData("7.25", false)]
    [InlineData("12.5cm", true)]
    [InlineData("no number", true)]
    [InlineData("", true)]
    public void DistanceText_MatchesLegacyConversion(string text, bool inches)
    {
        Assert.Equal(LegacyUnitDetailRules.ConvertDistanceText(text, inches), DistanceText.Convert(text, inches));
    }

    [Theory]
    [MemberData(nameof(CorpusSources))]
    public void EntryDisplayNames_MatchLegacyDisplayNameContext(string source, bool inches)
    {
        var reader = new ArmyJsonReader();
        var mismatches = new List<string>();

        foreach (var unit in ReferenceCorpus.All.Where(x => x.Source == source))
        {
            var lookups = CorpusLookups.For(unit.ArmyFiltersJson);
            var legacy = LegacyDisplayNameContext.Create(unit.ArmyFiltersJson, inches, LegacySharedUtilities.TryParseId);
            var names = new ProfileEntryNames(reader.ReadArmyFilters(unit.ArmyFiltersJson).Extras, inches);
            var groups = reader.ReadProfileGroups(unit.ProfileGroupsJson);

            using var doc = JsonDocument.Parse(unit.ProfileGroupsJson);
            var root = doc.RootElement;
            var legacyOptions = root.EnumerateArray().SelectMany(g => g.GetProperty("options").EnumerateArray()).ToList();
            var options = groups.AllOptions().Select(x => x.Option).ToList();
            Assert.Equal(legacyOptions.Count, options.Count);

            foreach (var (property, kind) in Kinds)
            {
                var lookup = LegacyProfileSnapshot.LookupFor(property, lookups);
                Compare(mismatches, unit, $"common-profiles[{property}]",
                    legacy.ComputeCommonDisplayNamesFromProfiles(unit.ProfileGroupsJson, property, lookup),
                    names.CommonAcrossProfiles(groups, kind, lookup));
                Compare(mismatches, unit, $"common-options[{property}]",
                    legacy.IntersectDisplayNamesWithIncludes(root, legacyOptions, property, lookup),
                    names.CommonAcrossOptions(groups, options, kind, lookup));

                for (var i = 0; i < options.Count; i++)
                {
                    var legacyEntries = LegacyCompanyProfileOptionService.GetOptionEntriesWithIncludes(root, legacyOptions[i], property).ToList();
                    var entries = groups.GetEntriesWithIncludes(options[i], kind);
                    Compare(mismatches, unit, $"ordered[{i},{property}]",
                        legacy.GetOrderedIdDisplayNamesFromEntries(legacyEntries, lookup),
                        names.OrderedUnique(entries, lookup));
                    Compare(mismatches, unit, $"counted[{i},{property}]",
                        legacy.GetCountedDisplayNamesFromEntries(legacyEntries, lookup),
                        names.Counted(entries, lookup));
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
    [MemberData(nameof(CorpusSources))]
    public void TraitsStatlinesAndPeripherals_MatchLegacyRules(string source, bool forceLieutenant)
    {
        var reader = new ArmyJsonReader();
        var mismatches = new List<string>();

        foreach (var unit in ReferenceCorpus.All.Where(x => x.Source == source))
        {
            var lookups = CorpusLookups.For(unit.ArmyFiltersJson);
            var groups = reader.ReadProfileGroups(unit.ProfileGroupsJson);
            using var doc = JsonDocument.Parse(unit.ProfileGroupsJson);
            var root = doc.RootElement;

            var legacyOrder = LegacyUnitDetailRules.ParseUnitOrderTraits(root);
            var order = UnitTraits.ReadOrderTraits(groups);
            Compare(mismatches, unit, "order-traits",
                $"{legacyOrder.HasRegular}{legacyOrder.HasIrregular}{legacyOrder.HasImpetuous}{legacyOrder.HasTacticalAwareness}",
                $"{order.HasRegular}{order.HasIrregular}{order.HasImpetuous}{order.HasTacticalAwareness}");
            var legacyTech = LegacyUnitDetailRules.ParseUnitTechTraits(root, lookups.Equip, lookups.Skills, lookups.Chars);
            var tech = UnitTraits.ReadTechTraits(groups, lookups.Equip, lookups.Skills, lookups.Chars);
            Compare(mismatches, unit, "tech-traits",
                $"{legacyTech.HasCube}{legacyTech.HasCube2}{legacyTech.HasHackable}",
                $"{tech.HasCube}{tech.HasCube2}{tech.HasHackable}");

            var legacyProfiles = root.EnumerateArray()
                .SelectMany(g => g.TryGetProperty("profiles", out var p) ? p.EnumerateArray() : Enumerable.Empty<JsonElement>())
                .ToList();
            var profiles = groups.Groups.SelectMany(g => g.Profiles).ToList();
            for (var i = 0; i < Math.Min(legacyProfiles.Count, profiles.Count); i++)
            {
                Compare(mismatches, unit, $"statline[{i}]", LegacyStatline(legacyProfiles[i]), Statline(profiles[i]));
            }

            foreach (var name in PeripheralNames(groups, lookups))
            {
                var legacyFound = LegacyUnitDetailRules.TryFindPeripheralStatElement(root, name, out var legacyProfile);
                var profile = groups.FindPeripheralProfile(name);
                Compare(mismatches, unit, $"peripheral-profile['{name}']",
                    legacyFound ? LegacyStatline(legacyProfile) : "none",
                    profile is null ? "none" : Statline(profile));
            }

            foreach (var lieutenantOnly in (IEnumerable<bool>)[false, true])
            {
                var legacyFirst = LegacyUnitDetailRules.FindFirstVisiblePeripheralProfile(new LegacyPeripheralSelectionRequest
                {
                    ProfileGroupsRoot = root,
                    PeripheralLookup = lookups.Peripheral,
                    ForceLieutenant = forceLieutenant,
                    LieutenantOnlyUnits = lieutenantOnly,
                    SkillsLookup = lookups.Skills,
                    IsLieutenantOption = LegacySharedUtilities.IsLieutenantOption,
                    TryParseId = LegacySharedUtilities.TryParseId
                });
                var first = groups.FindFirstVisiblePeripheral(lookups.Peripheral, lookups.Skills, forceLieutenant, lieutenantOnly);
                Compare(mismatches, unit, $"first-peripheral[lt-only={lieutenantOnly}]",
                    legacyFirst is null ? "none" : $"{legacyFirst.PeripheralName}: {LegacyStatline(legacyFirst.PeripheralProfile)}",
                    first is null ? "none" : $"{first.Value.Name}: {Statline(first.Value.Profile)}");
            }

            if (mismatches.Count > 20)
            {
                break;
            }
        }

        Assert.True(mismatches.Count == 0, string.Join(Environment.NewLine, mismatches));
    }

    [Theory]
    [InlineData("""{"move":"10-5","cc":13}""")]
    [InlineData("""{"Move":["15","x",5]}""")]
    [InlineData("""{"mov":"4 - 4"}""")]
    [InlineData("""{"move":null,"mov":[1,2]}""")]
    [InlineData("""{"str":4,"w":2,"ava":255}""")]
    [InlineData("""{"str":true,"vita":"3","ava":"-1"}""")]
    [InlineData("""{"stats":{"w":1},"AVA":2}""")]
    public void SyntheticStatlines_MatchLegacyRules(string profileJson)
    {
        using var doc = JsonDocument.Parse(profileJson);
        var groups = new ArmyJsonReader().ReadProfileGroups($$"""[{"id":1,"profiles":[{{profileJson}}],"options":[]}]""");

        Assert.Equal(LegacyStatline(doc.RootElement), Statline(groups.Groups[0].Profiles[0]));
    }

    private static IEnumerable<string> PeripheralNames(UnitProfileGroups groups, CorpusLookups lookups)
    {
        foreach (var group in groups.Groups)
        {
            yield return group.Isc ?? string.Empty;
            foreach (var option in group.Options)
            {
                yield return option.Name ?? string.Empty;
                foreach (var entry in option.Peripheral)
                {
                    if (entry.Id is int id && lookups.Peripheral.TryGetValue(id, out var name))
                    {
                        yield return name;
                    }
                }
            }
        }

        yield return "No Such Peripheral";
    }

    private static string LegacyStatline(JsonElement profile)
    {
        var move = LegacyUnitDetailRules.ReadMoveValue(profile);
        var (header, value) = LegacyUnitDetailRules.ReadVitality(profile);
        return $"id={(profile.TryGetProperty("id", out var id) && LegacySharedUtilities.TryParseId(id, out var parsed) ? parsed.ToString() : "?")} " +
               $"move={move.FirstCm}/{move.SecondCm} ava={LegacyUnitDetailRules.ReadAvaAsString(profile)} {header}={value} " +
               $"stats={LegacyUnitDetailRules.HasStatFields(profile)}";
    }

    private static string Statline(UnitProfile profile)
    {
        var (first, second) = profile.MoveFirstSecondCm;
        var (header, value) = profile.Vitality;
        return $"id={(profile.Id.HasValue ? profile.Id.Value.ToString() : "?")} " +
               $"move={first}/{second} ava={profile.AvaText} {header}={value} " +
               $"stats={profile.HasStatFields}";
    }

    private static void Compare(List<string> mismatches, CorpusUnit unit, string what, IReadOnlyList<string> legacy, IReadOnlyList<string> actual)
    {
        Compare(mismatches, unit, what, string.Join(" | ", legacy), string.Join(" | ", actual));
    }

    private static void Compare(List<string> mismatches, CorpusUnit unit, string what, string legacy, string actual)
    {
        if (legacy != actual)
        {
            mismatches.Add($"{unit.Key} {what}: legacy='{legacy}' domain='{actual}'");
        }
    }
}
