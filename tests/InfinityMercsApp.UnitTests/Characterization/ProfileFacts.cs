using System.Globalization;
using System.Text;
using System.Text.Json;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.UnitTests.Legacy;

namespace InfinityMercsApp.UnitTests.Characterization;

/// <summary>
/// Renders the data-level facts about a unit (ids, quantities, costs, includes, peripherals, stats...) in
/// one text format from two sources: the frozen legacy rules over raw JSON, and <see cref="ArmyJsonReader"/>
/// plus the domain models. Equal text means the typed path reproduces the legacy behaviour.
/// </summary>
internal static class ProfileFacts
{
    private static readonly IReadOnlyList<(string Property, ProfileEntryKind Kind)> EntryKinds =
    [
        ("chars", ProfileEntryKind.Chars),
        ("skills", ProfileEntryKind.Skills),
        ("equip", ProfileEntryKind.Equip),
        ("weapons", ProfileEntryKind.Weapons),
        ("peripheral", ProfileEntryKind.Peripheral),
        ("ammunition", ProfileEntryKind.Ammunition),
        ("ammo", ProfileEntryKind.Ammo)
    ];

    private static readonly IReadOnlyList<string> StatNames = ["cc", "bs", "ph", "wip", "arm", "bts", "w", "s", "ava"];

    public static string FromLegacy(CorpusUnit unit)
    {
        var skills = CorpusLookups.For(unit.ArmyFiltersJson).Skills;
        var sb = new StringBuilder();
        sb.Append("factions ").AppendLine(Join(LegacySharedUtilities.ParseFactionIds(unit.FactionsJson).Select(Int)));
        sb.Append("has-lieutenant ").AppendLine(Bool(LegacyCompanyUnitFilterService.UnitHasLieutenantOption(unit.ProfileGroupsJson, skills)));

        using var doc = JsonDocument.Parse(unit.ProfileGroupsJson);
        var root = doc.RootElement;
        foreach (var group in root.EnumerateArray())
        {
            sb.Append("group ").Append(LegacyId(group))
                .Append(" isc=").Append(LegacySharedUtilities.ReadString(group, "isc", "-"))
                .Append(" controller=").AppendLine(Bool(LegacyCompanyProfileOptionService.IsControllerGroup(root, group)));

            foreach (var profile in LegacySharedUtilities.GetContainerEntries(group, "profiles"))
            {
                sb.Append(" profile ").Append(LegacyId(profile))
                    .Append(" name=").Append(LegacySharedUtilities.ReadString(profile, "name", "-"))
                    .Append(" type=").Append(Int(LegacySharedUtilities.ReadInt(profile, "type", -1)))
                    .Append(" str=").Append(Bool(LegacySharedUtilities.ReadBool(profile, "str", false)))
                    .Append(" mov=").Append(LegacySharedUtilities.ReadMoveFromProfile(profile));
                foreach (var stat in StatNames)
                {
                    sb.Append(' ').Append(stat).Append('=').Append(LegacySharedUtilities.ReadIntAsString(profile, stat));
                }

                sb.AppendLine();
                foreach (var (property, _) in EntryKinds)
                {
                    // Profiles read their arrays with a plain, case-sensitive property lookup.
                    var entries = profile.TryGetProperty(property, out var array) && array.ValueKind == JsonValueKind.Array
                        ? array.EnumerateArray().ToList()
                        : [];
                    AppendLegacyEntries(sb, "  " + property, entries);
                }
            }

            foreach (var option in LegacySharedUtilities.GetContainerEntries(group, "options"))
            {
                var adjusted = LegacyCompanyProfileOptionService.ReadAdjustedOptionCost(root, group, option);
                sb.Append(" option ").Append(LegacyId(option))
                    .Append(" name=").Append(LegacySharedUtilities.ReadString(option, "name", "-"))
                    .Append(" swc=").Append(LegacyCompanyProfileOptionService.ReadOptionSwc(option))
                    .Append(" positive-swc=").Append(Bool(LegacySharedUtilities.IsPositiveSwc(LegacyCompanyProfileOptionService.ReadOptionSwc(option))))
                    .Append(" cost=").Append(LegacyCompanyProfileOptionService.ReadOptionCost(option))
                    .Append(" adjusted=").Append(adjusted)
                    .Append(" adjusted-value=").Append(Int(LegacyCompanyUnitFilterService.ParseCostValue(adjusted)))
                    .Append(" minis=").Append(Int(LegacyCompanyProfileOptionService.ReadOptionMinis(option)))
                    .Append(" lieutenant=").Append(Bool(LegacySharedUtilities.IsLieutenantOption(option, skills)))
                    .Append(" lieutenant-order=").AppendLine(Bool(LegacySharedUtilities.HasLieutenantOrder(option)));
                foreach (var (property, _) in EntryKinds)
                {
                    AppendLegacyEntries(sb, "  " + property + "+includes",
                        LegacyCompanyProfileOptionService.GetOptionEntriesWithIncludes(root, option, property).ToList());
                }

                AppendLegacyEntries(sb, "  display-peripherals",
                    LegacyCompanyProfileOptionService.GetDisplayPeripheralEntriesForOption(root, group, option).ToList());
            }
        }

        return sb.ToString();
    }

    public static string FromReader(CorpusUnit unit, IArmyJsonReader reader)
    {
        var skills = reader.ReadArmyFilters(unit.ArmyFiltersJson).Names(ArmyFilterSection.Skills);
        var groups = reader.ReadProfileGroups(unit.ProfileGroupsJson);
        var sb = new StringBuilder();
        sb.Append("factions ").AppendLine(Join(reader.ReadFactionIds(unit.FactionsJson).Select(Int)));
        sb.Append("has-lieutenant ").AppendLine(Bool(groups.HasLieutenantOption(skills)));

        foreach (var group in groups.Groups)
        {
            sb.Append("group ").Append(Id(group.Id))
                .Append(" isc=").Append(TextOrDash(group.Isc))
                .Append(" controller=").AppendLine(Bool(groups.IsControllerGroup(group)));

            foreach (var profile in group.Profiles)
            {
                sb.Append(" profile ").Append(Id(profile.Id))
                    .Append(" name=").Append(TextOrDash(profile.Name))
                    .Append(" type=").Append(Int(profile.Type ?? -1))
                    .Append(" str=").Append(Bool(profile.Str))
                    .Append(" mov=").Append(profile.Move);
                IEnumerable<(string Stat, int? Value)> stats =
                [
                    ("cc", profile.Cc), ("bs", profile.Bs), ("ph", profile.Ph), ("wip", profile.Wip),
                    ("arm", profile.Arm), ("bts", profile.Bts), ("w", profile.W), ("s", profile.S), ("ava", profile.Ava)
                ];
                foreach (var (stat, value) in stats)
                {
                    sb.Append(' ').Append(stat).Append('=').Append(UnitProfile.StatText(value));
                }

                sb.AppendLine();
                foreach (var (property, kind) in EntryKinds)
                {
                    AppendEntries(sb, "  " + property, profile.Entries(kind));
                }
            }

            foreach (var option in group.Options)
            {
                var adjusted = groups.GetAdjustedCost(group, option);
                sb.Append(" option ").Append(Id(option.Id))
                    .Append(" name=").Append(TextOrDash(option.Name))
                    .Append(" swc=").Append(option.Swc)
                    .Append(" positive-swc=").Append(Bool(option.HasPositiveSwc))
                    .Append(" cost=").Append(option.Cost)
                    .Append(" adjusted=").Append(adjusted)
                    .Append(" adjusted-value=").Append(Int(groups.GetAdjustedCostValue(group, option)))
                    .Append(" minis=").Append(Int(option.Minis))
                    .Append(" lieutenant=").Append(Bool(option.IsLieutenant(skills)))
                    .Append(" lieutenant-order=").AppendLine(Bool(option.HasLieutenantOrder));
                foreach (var (property, kind) in EntryKinds)
                {
                    AppendEntries(sb, "  " + property + "+includes", groups.GetEntriesWithIncludes(option, kind));
                }

                AppendEntries(sb, "  display-peripherals", groups.GetDisplayPeripheralEntries(group, option));
            }
        }

        return sb.ToString();
    }

    private static void AppendLegacyEntries(StringBuilder sb, string label, IReadOnlyList<JsonElement> entries)
    {
        if (entries.Count == 0)
        {
            return;
        }

        var formatted = entries.Select(entry =>
        {
            int? id = LegacySharedUtilities.TryParseId(entry, out var parsed) ? parsed : null;
            var quantity = LegacyCompanyProfileOptionService.ReadEntryQuantity(entry);

            // Same inline rules the legacy viewer uses for "order" and "extra".
            int? order = entry.ValueKind == JsonValueKind.Object &&
                         entry.TryGetProperty("order", out var orderElement) &&
                         orderElement.ValueKind == JsonValueKind.Number &&
                         orderElement.TryGetInt32(out var parsedOrder)
                ? parsedOrder
                : null;
            var extras = new List<int>();
            if (entry.ValueKind == JsonValueKind.Object &&
                entry.TryGetProperty("extra", out var extraElement) &&
                extraElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var extra in extraElement.EnumerateArray())
                {
                    if (LegacySharedUtilities.TryParseId(extra, out var extraId))
                    {
                        extras.Add(extraId);
                    }
                }
            }

            return FormatEntry(id, quantity, order, extras);
        });
        sb.Append(label).Append(' ').AppendLine(Join(formatted));
    }

    private static void AppendEntries(StringBuilder sb, string label, IReadOnlyList<ProfileEntry> entries)
    {
        if (entries.Count == 0)
        {
            return;
        }

        sb.Append(label).Append(' ')
            .AppendLine(Join(entries.Select(entry => FormatEntry(entry.Id, entry.Quantity, entry.Order, entry.Extras))));
    }

    private static string FormatEntry(int? id, int quantity, int? order, IReadOnlyList<int> extras) =>
        $"{Id(id)}x{Int(quantity)}@{(order.HasValue ? Int(order.Value) : "-")}{Join(extras.Select(Int))}";

    private static string LegacyId(JsonElement element) =>
        element.TryGetProperty("id", out var id) && LegacySharedUtilities.TryParseId(id, out var parsed) ? Int(parsed) : "?";

    private static string Id(int? id) => id.HasValue ? Int(id.Value) : "?";

    private static string TextOrDash(string? text) => string.IsNullOrWhiteSpace(text) ? "-" : text;

    private static string Join(IEnumerable<string> values) => "[" + string.Join(",", values) + "]";

    private static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Bool(bool value) => value ? "true" : "false";
}
