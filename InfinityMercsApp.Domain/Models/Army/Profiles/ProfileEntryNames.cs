using System.Globalization;
using System.Text.RegularExpressions;

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

    private string FormatExtra(ExtraDefinition definition) =>
        definition.IsDistance ? DistanceText.Convert(definition.Name, _showUnitsInInches) : definition.Name;
}
