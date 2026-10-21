using InfinityMercsApp.Domain.Models.Filtering;

namespace InfinityMercsApp.Domain.Models.Army.Profiles;

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

        foreach (var skillName in ProfileEntryNames.PlainNamesInApiOrder(option.Skills, skillNames).Select(entry => entry.Name))
        {
            if (IsDetailTag(skillName) && !normalizedBase.Contains(skillName.ToLowerInvariant()))
            {
                details.Add(skillName);
            }
        }

        foreach (var equipName in ProfileEntryNames.PlainNamesInApiOrder(option.Equip, equipNames).Select(entry => entry.Name))
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
        foreach (var name in ProfileEntryNames.PlainNamesInApiOrder(entries, lookup).Select(entry => entry.Name))
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                target.Add(name.Trim());
            }
        }
    }
}
