using InfinityMercsApp.Domain.Models.Army.Filters;

namespace InfinityMercsApp.Domain.Models.Army.Profiles;

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
    public List<NamedEntry> InApiOrder(IEnumerable<ProfileEntry> entries, IReadOnlyDictionary<int, string> lookup) =>
        InApiOrder(entries, entry => DisplayName(entry, lookup)!);

    /// <summary>
    /// Display names with summed quantities (e.g. <c>"Drone (2)"</c>) and the id of the first entry of each,
    /// sorted alphabetically.
    /// </summary>
    public List<NamedEntry> CountedWithIds(IEnumerable<ProfileEntry> entries, IReadOnlyDictionary<int, string> lookup)
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
            .Select(x => new NamedEntry(x.Value.Id, $"{x.Key} ({x.Value.Count})"))
            .ToList();
    }

    /// <summary>
    /// Plain looked-up names (the id itself when unknown, no extras) with their ids, in API order.
    /// </summary>
    public static List<NamedEntry> PlainNamesInApiOrder(IEnumerable<ProfileEntry> entries, IReadOnlyDictionary<int, string> lookup) =>
        InApiOrder(entries, entry => lookup.TryGetValue(entry.Id!.Value, out var name) ? name : entry.Id!.Value.ToString());

    // Entries with an id, by API order and then by name (entries without an order sort last).
    private static List<NamedEntry> InApiOrder(IEnumerable<ProfileEntry> entries, Func<ProfileEntry, string> nameOf) =>
        entries
            .Where(entry => entry.Id.HasValue)
            .Select(entry => new { entry.SortOrder, Entry = new NamedEntry(entry.Id!.Value, nameOf(entry)) })
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Entry.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Entry)
            .ToList();

    private string FormatExtra(ExtraDefinition definition) =>
        definition.IsDistance ? DistanceText.Convert(definition.Name, _showUnitsInInches) : definition.Name;
}
