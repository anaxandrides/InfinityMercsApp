using System.Globalization;
using System.Text;
using System.Text.Json;
using InfinityMercsApp.UnitTests.Legacy;

namespace InfinityMercsApp.UnitTests.Characterization;

/// <summary>
/// Renders everything the legacy JSON rules derive from one unit's <c>ProfileGroupsJson</c> as stable text.
/// Every value in the output comes from a legacy oracle function, so the text is a record of current behaviour.
/// </summary>
internal static class LegacyProfileSnapshot
{
    private static readonly IReadOnlyList<string> EntryProperties = ["chars", "skills", "equip", "weapons", "peripheral", "ammunition", "ammo"];

    public static string Build(CorpusUnit unit)
    {
        var lookups = CorpusLookups.For(unit.ArmyFiltersJson);
        var sb = new StringBuilder();
        sb.Append("unit ").AppendLine(unit.Key);
        sb.Append("  has-lieutenant-option: ")
            .AppendLine(Bool(LegacyCompanyUnitFilterService.UnitHasLieutenantOption(unit.ProfileGroupsJson, lookups.Skills)));
        sb.Append("  has-visible-option(zero-swc): ")
            .AppendLine(Bool(LegacyCompanyUnitFilterService.UnitHasVisibleOption(unit.ProfileGroupsJson, lookups.Skills, false, true)));
        sb.Append("  faction-ids: ")
            .AppendLine(Join(LegacySharedUtilities.ParseFactionIds(unit.FactionsJson).Select(Int)));

        using var doc = JsonDocument.Parse(unit.ProfileGroupsJson);
        var root = doc.RootElement;
        foreach (var group in root.EnumerateArray())
        {
            sb.Append("  group ").Append(Id(group))
                .Append(" isc=").Append(Quote(LegacySharedUtilities.ReadString(group, "isc", "-")))
                .Append(" controller=").Append(Bool(LegacyCompanyProfileOptionService.IsControllerGroup(root, group)))
                .AppendLine();

            if (group.TryGetProperty("profiles", out var profiles) && profiles.ValueKind == JsonValueKind.Array)
            {
                foreach (var profile in profiles.EnumerateArray())
                {
                    sb.Append("    profile ").Append(Id(profile))
                        .Append(" name=").Append(Quote(LegacySharedUtilities.ReadString(profile, "name", "-")))
                        .Append(" mov=").Append(LegacySharedUtilities.ReadMoveFromProfile(profile))
                        .Append(" cc=").Append(LegacySharedUtilities.ReadIntAsString(profile, "cc"))
                        .Append(" bs=").Append(LegacySharedUtilities.ReadIntAsString(profile, "bs"))
                        .Append(" ph=").Append(LegacySharedUtilities.ReadIntAsString(profile, "ph"))
                        .Append(" wip=").Append(LegacySharedUtilities.ReadIntAsString(profile, "wip"))
                        .Append(" arm=").Append(LegacySharedUtilities.ReadIntAsString(profile, "arm"))
                        .Append(" bts=").Append(LegacySharedUtilities.ReadIntAsString(profile, "bts"))
                        .Append(" w=").Append(LegacySharedUtilities.ReadIntAsString(profile, "w"))
                        .Append(" s=").Append(LegacySharedUtilities.ReadIntAsString(profile, "s"))
                        .Append(" ava=").Append(LegacySharedUtilities.ReadIntAsString(profile, "ava"))
                        .Append(" type=").Append(LegacySharedUtilities.ReadInt(profile, "type", -1).ToString(CultureInfo.InvariantCulture))
                        .Append(" str=").Append(Bool(LegacySharedUtilities.ReadBool(profile, "str", false)))
                        .AppendLine();

                    foreach (var property in EntryProperties)
                    {
                        var entries = LegacySharedUtilities.GetContainerEntries(profile, property).ToList();
                        if (entries.Count > 0)
                        {
                            sb.Append("      ").Append(property).Append(": ")
                                .AppendLine(FormatEntries(entries, LookupFor(property, lookups), lookups));
                        }
                    }
                }
            }

            if (!group.TryGetProperty("options", out var options) || options.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var option in options.EnumerateArray())
            {
                sb.Append("    option ").Append(Id(option))
                    .Append(" name=").Append(Quote(LegacySharedUtilities.ReadString(option, "name", "-")))
                    .Append(" swc=").Append(LegacyCompanyProfileOptionService.ReadOptionSwc(option))
                    .Append(" cost=").Append(LegacyCompanyProfileOptionService.ReadOptionCost(option))
                    .Append(" adjusted-cost=").Append(LegacyCompanyProfileOptionService.ReadAdjustedOptionCost(root, group, option))
                    .Append(" minis=").Append(Int(LegacyCompanyProfileOptionService.ReadOptionMinis(option)))
                    .Append(" lieutenant=").Append(Bool(LegacySharedUtilities.IsLieutenantOption(option, lookups.Skills)))
                    .Append(" lieutenant-order=").Append(Bool(LegacySharedUtilities.HasLieutenantOrder(option)))
                    .AppendLine();

                foreach (var property in EntryProperties)
                {
                    var entries = LegacyCompanyProfileOptionService.GetOptionEntriesWithIncludes(root, option, property).ToList();
                    if (entries.Count > 0)
                    {
                        sb.Append("      ").Append(property).Append("+includes: ")
                            .AppendLine(FormatEntries(entries, LookupFor(property, lookups), lookups));
                    }
                }

                var displayPeripherals = LegacyCompanyProfileOptionService.GetDisplayPeripheralEntriesForOption(root, group, option);
                if (displayPeripherals.Count > 0)
                {
                    sb.Append("      display-peripherals: ")
                        .AppendLine(FormatEntries(displayPeripherals, lookups.Peripheral, lookups));
                }

                sb.Append("      summary: ")
                    .AppendLine(LegacyViewerOptionRules.BuildOptionConfigurationSummary(option, lookups.Weapons, lookups.Equip, lookups.Skills));
                sb.Append("      display-name: ")
                    .AppendLine(LegacyViewerOptionRules.BuildOptionDisplayName(
                        option,
                        LegacySharedUtilities.ReadString(option, "name", "-"),
                        lookups.Equip,
                        lookups.Skills));
            }
        }

        var characteristics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var equipment = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var weapons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ammo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        LegacyCompanyUnitFilterService.AddFilterOptionsFromVisibleProfilesAndOptions(
            unit.ProfileGroupsJson,
            lookups.Chars,
            lookups.Skills,
            lookups.Equip,
            lookups.Weapons,
            lookups.Ammo,
            requireLieutenant: false,
            requireZeroSwc: false,
            maxCost: null,
            includeProfileValues: true,
            characteristics,
            skills,
            equipment,
            weapons,
            ammo);
        sb.Append("  filter-options chars: ").AppendLine(Join(Sorted(characteristics)));
        sb.Append("  filter-options skills: ").AppendLine(Join(Sorted(skills)));
        sb.Append("  filter-options equip: ").AppendLine(Join(Sorted(equipment)));
        sb.Append("  filter-options weapons: ").AppendLine(Join(Sorted(weapons)));
        sb.Append("  filter-options ammo: ").AppendLine(Join(Sorted(ammo)));
        return sb.ToString();
    }

    internal static IReadOnlyDictionary<int, string> LookupFor(string property, CorpusLookups lookups) => property switch
    {
        "chars" => lookups.Chars,
        "skills" => lookups.Skills,
        "equip" => lookups.Equip,
        "weapons" => lookups.Weapons,
        "peripheral" => lookups.Peripheral,
        _ => lookups.Ammo
    };

    /// <summary>
    /// Formats entries as <c>id×quantity name</c> using legacy id/quantity rules, followed by the
    /// legacy viewer's ordered display names (which encode the <c>order</c> and <c>extra</c> fields).
    /// </summary>
    private static string FormatEntries(IEnumerable<JsonElement> entries, IReadOnlyDictionary<int, string> lookup, CorpusLookups lookups)
    {
        var list = entries.ToList();
        var raw = list.Select(entry =>
        {
            var id = LegacySharedUtilities.TryParseId(entry, out var parsed) ? Int(parsed) : "?";
            return $"{id}x{Int(LegacyCompanyProfileOptionService.ReadEntryQuantity(entry))}";
        });
        var ordered = LegacyViewerOptionRules.GetOrderedIdDisplayNamesFromEntries(list, lookup, lookups.Extras, showUnitsInInches: false)
            .Select(x => x.Name);
        return $"{Join(raw)} | {Join(ordered)}";
    }

    private static string Id(JsonElement element) =>
        element.TryGetProperty("id", out var id) && LegacySharedUtilities.TryParseId(id, out var parsed) ? Int(parsed) : "?";

    private static IEnumerable<string> Sorted(IEnumerable<string> values) =>
        values.OrderBy(x => x, StringComparer.Ordinal);

    private static string Join(IEnumerable<string> values) => "[" + string.Join(", ", values) + "]";

    private static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Bool(bool value) => value ? "true" : "false";

    private static string Quote(string value) => "\"" + value + "\"";
}
