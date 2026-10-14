using System.Globalization;
using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Models.Filtering;

namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>
/// Converts a distance such as <c>"+5cm"</c>, <c>"10"</c> or <c>"4\""</c> to the display unit.
/// </summary>
public static class DistanceText
{
    public static string Convert(string distanceText, bool showUnitsInInches)
    {
        if (string.IsNullOrWhiteSpace(distanceText))
        {
            return distanceText;
        }

        var match = Regex.Match(
            distanceText,
            @"([+-]?)(\d+(?:\.\d+)?)(?:\s*(cm|""|in|inch|inches))?",
            RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return distanceText;
        }

        var sign = match.Groups[1].Value;
        var numberText = match.Groups[2].Value;
        if (!double.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
        {
            return distanceText;
        }

        var unitToken = match.Groups[3].Success
            ? match.Groups[3].Value.Trim().ToLowerInvariant()
            : string.Empty;
        var valueInCm = unitToken is "\"" or "in" or "inch" or "inches"
            ? parsedValue * 2.5
            : parsedValue;

        string replacement;
        if (showUnitsInInches)
        {
            var inches = (int)Math.Round(valueInCm / 2.5, MidpointRounding.AwayFromZero);
            replacement = $"{sign}{inches}\"";
        }
        else
        {
            var roundedCm = Math.Round(valueInCm, 2, MidpointRounding.AwayFromZero);
            var cmText = Math.Abs(roundedCm - Math.Round(roundedCm)) < 0.001
                ? ((int)Math.Round(roundedCm)).ToString(CultureInfo.InvariantCulture)
                : roundedCm.ToString("0.##", CultureInfo.InvariantCulture);
            replacement = $"{sign}{cmText}cm";
        }

        return distanceText.Remove(match.Index, match.Length).Insert(match.Index, replacement);
    }
}

/// <summary>
/// Turns profile entries into display names: the looked-up name (or the id when unknown) followed by the
/// entry's extras, e.g. <c>"Forward Observer"</c>, <c>"Mimetism (-6)"</c>, <c>"Sensor (+8cm)"</c>.
/// </summary>
public sealed class ProfileEntryNames
{
    private readonly IReadOnlyDictionary<int, ExtraDefinition> _extras;
    private readonly bool _showUnitsInInches;

    public ProfileEntryNames(IReadOnlyDictionary<int, ExtraDefinition> extras, bool showUnitsInInches)
    {
        _extras = extras;
        _showUnitsInInches = showUnitsInInches;
    }

    /// <summary>The display name of one entry with an id; <c>null</c> when the entry has no id.</summary>
    public string? DisplayName(ProfileEntry entry, IReadOnlyDictionary<int, string> lookup)
    {
        if (entry.Id is not int id)
        {
            return null;
        }

        var baseName = lookup.TryGetValue(id, out var resolvedName) ? resolvedName : id.ToString();
        if (entry.Extras.Count == 0)
        {
            return baseName;
        }

        var extras = entry.Extras
            .Select(extraId => _extras.TryGetValue(extraId, out var definition) && !string.IsNullOrWhiteSpace(definition.Name)
                ? FormatExtra(definition)
                : extraId.ToString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return extras.Count == 0 ? baseName : $"{baseName} ({string.Join(", ", extras)})";
    }

    /// <summary>Distinct display names, sorted alphabetically (case-insensitive).</summary>
    public List<string> OrderedUnique(IEnumerable<ProfileEntry> entries, IReadOnlyDictionary<int, string> lookup) =>
        entries
            .Select(entry => DisplayName(entry, lookup))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Display names with summed quantities, e.g. <c>"Drone (2)"</c>, sorted alphabetically.</summary>
    public List<string> Counted(IEnumerable<ProfileEntry> entries, IReadOnlyDictionary<int, string> lookup)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            var displayName = DisplayName(entry, lookup);
            if (string.IsNullOrWhiteSpace(displayName))
            {
                continue;
            }

            counts[displayName] = counts.TryGetValue(displayName, out var existing)
                ? existing + entry.Quantity
                : entry.Quantity;
        }

        return counts
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => $"{x.Key} ({x.Value})")
            .ToList();
    }

    /// <summary>
    /// Display names every profile shares, looking only at controller groups when the unit has any.
    /// Empty when there are no profiles.
    /// </summary>
    public List<string> CommonAcrossProfiles(UnitProfileGroups groups, ProfileEntryKind kind, IReadOnlyDictionary<int, string> lookup)
    {
        var controllerGroups = groups.Groups.Where(groups.IsControllerGroup).ToList();
        var groupsToProcess = controllerGroups.Count > 0 ? controllerGroups : groups.Groups.ToList();

        HashSet<string>? common = null;
        foreach (var profile in groupsToProcess.SelectMany(group => group.Profiles))
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in profile.Entries(kind))
            {
                if (DisplayName(entry, lookup) is { } name)
                {
                    names.Add(name);
                }
            }

            if (common is null)
            {
                common = names;
            }
            else
            {
                common.IntersectWith(names);
            }
        }

        return common is null || common.Count == 0
            ? []
            : common.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Display names every given option grants, counting the options it includes.</summary>
    public List<string> CommonAcrossOptions(UnitProfileGroups groups, IEnumerable<ProfileOption> options, ProfileEntryKind kind, IReadOnlyDictionary<int, string> lookup)
    {
        HashSet<string>? common = null;
        foreach (var option in options)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in groups.GetEntriesWithIncludes(option, kind))
            {
                if (DisplayName(entry, lookup) is { } name)
                {
                    names.Add(name);
                }
            }

            if (common is null)
            {
                common = names;
            }
            else
            {
                common.IntersectWith(names);
            }
        }

        return common is null || common.Count == 0
            ? []
            : common.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Display names with their ids in API order (entries without an order last, then by name). Not de-duplicated.
    /// </summary>
    public List<(int Id, string Name)> InApiOrder(IEnumerable<ProfileEntry> entries, IReadOnlyDictionary<int, string> lookup) =>
        entries
            .Where(entry => entry.Id.HasValue)
            .Select(entry => (entry.SortOrder, Id: entry.Id!.Value, Name: DisplayName(entry, lookup)!))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => (x.Id, x.Name))
            .ToList();

    /// <summary>
    /// Display names with summed quantities (e.g. <c>"Drone (2)"</c>) and the id of the first entry of each,
    /// sorted alphabetically.
    /// </summary>
    public List<(int Id, string Name)> CountedWithIds(IEnumerable<ProfileEntry> entries, IReadOnlyDictionary<int, string> lookup)
    {
        var counts = new Dictionary<string, (int Id, int Count)>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (entry.Id is not int id || DisplayName(entry, lookup) is not { } displayName || string.IsNullOrWhiteSpace(displayName))
            {
                continue;
            }

            counts[displayName] = counts.TryGetValue(displayName, out var existing)
                ? (existing.Id, existing.Count + entry.Quantity)
                : (id, entry.Quantity);
        }

        return counts
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => (x.Value.Id, $"{x.Key} ({x.Value.Count})"))
            .ToList();
    }

    /// <summary>
    /// Plain looked-up names (the id itself when unknown, no extras) with their ids, in API order.
    /// </summary>
    public static List<(int Id, string Name)> PlainNamesInApiOrder(IEnumerable<ProfileEntry> entries, IReadOnlyDictionary<int, string> lookup) =>
        entries
            .Where(entry => entry.Id.HasValue)
            .Select(entry => (entry.SortOrder, Id: entry.Id!.Value, Name: lookup.TryGetValue(entry.Id!.Value, out var name) ? name : entry.Id!.Value.ToString()))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => (x.Id, x.Name))
            .ToList();

    private string FormatExtra(ExtraDefinition definition) =>
        definition.IsDistance ? DistanceText.Convert(definition.Name, _showUnitsInInches) : definition.Name;
}

/// <summary>
/// Option names as the unit viewer shows them.
/// </summary>
public static class ViewerOptionNames
{
    private static readonly IReadOnlyList<string> DetailTagTokens =
    [
        "forward observer", "hacker", "hacking device", "specialist", "paramedic",
        "doctor", "engineer", "lieutenant", "nco", "chain of command"
    ];

    /// <summary>
    /// The option name followed by its role skills/equipment not already in the name, in API order, e.g.
    /// <c>"FUSILIER (Forward Observer, Lieutenant)"</c>.
    /// </summary>
    public static string WithDetailTags(
        ProfileOption option,
        string baseName,
        IReadOnlyDictionary<int, string> equipNames,
        IReadOnlyDictionary<int, string> skillNames)
    {
        var details = new List<string>();
        var normalizedBase = baseName.ToLowerInvariant();

        foreach (var (_, skillName) in ProfileEntryNames.PlainNamesInApiOrder(option.Skills, skillNames))
        {
            if (IsDetailTag(skillName) && !normalizedBase.Contains(skillName.ToLowerInvariant()))
            {
                details.Add(skillName);
            }
        }

        foreach (var (_, equipName) in ProfileEntryNames.PlainNamesInApiOrder(option.Equip, equipNames))
        {
            if (IsDetailTag(equipName) && !normalizedBase.Contains(equipName.ToLowerInvariant()))
            {
                details.Add(equipName);
            }
        }

        foreach (var order in option.Orders)
        {
            if (order.IsLieutenant && !normalizedBase.Contains("lieutenant"))
            {
                details.Add("Lieutenant");
            }
        }

        var distinctDetails = details.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return distinctDetails.Count == 0 ? baseName : $"{baseName} ({string.Join(", ", distinctDetails)})";
    }

    /// <summary>True for role names worth showing next to an option name (Hacker, Doctor, Lieutenant...).</summary>
    public static bool IsDetailTag(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        DetailTagTokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Adds every name a unit's options (own entries, without includes) and profiles (characteristics,
    /// skills, equipment) refer to — unknown ids as the id itself — and returns the highest option cost.
    /// Used to populate the unit viewer's filter popup.
    /// </summary>
    public static int CollectAllOptionNames(
        UnitProfileGroups groups,
        UnitFilterLookups lookups,
        UnitFilterOptionSets names)
    {
        var maxCost = 0;
        foreach (var group in groups.Groups)
        {
            foreach (var option in group.Options)
            {
                maxCost = Math.Max(maxCost, groups.GetAdjustedCostValue(group, option));
                AddPlainNames(option.Chars, lookups.Chars, names.Characteristics);
                AddPlainNames(option.Skills, lookups.Skills, names.Skills);
                AddPlainNames(option.Equip, lookups.Equip, names.Equipment);
                AddPlainNames(option.Weapons, lookups.Weapons, names.Weapons);
                AddPlainNames(option.Ammunition, lookups.Ammo, names.Ammo);
                AddPlainNames(option.Ammo, lookups.Ammo, names.Ammo);
            }

            foreach (var profile in group.Profiles)
            {
                AddPlainNames(profile.Chars, lookups.Chars, names.Characteristics);
                AddPlainNames(profile.Skills, lookups.Skills, names.Skills);
                AddPlainNames(profile.Equip, lookups.Equip, names.Equipment);
            }
        }

        return maxCost;
    }

    private static void AddPlainNames(IEnumerable<ProfileEntry> entries, IReadOnlyDictionary<int, string> lookup, ISet<string> target)
    {
        foreach (var (_, name) in ProfileEntryNames.PlainNamesInApiOrder(entries, lookup))
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                target.Add(name.Trim());
            }
        }
    }
}
