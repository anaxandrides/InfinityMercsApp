using System.Globalization;

namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>
/// Everything a unit's <c>profileGroups</c> payload describes, parsed once into typed data, plus the rules
/// that need the whole unit to answer (options that include other options, controllers and their peripherals,
/// peripheral-adjusted costs).
/// </summary>
public sealed class UnitProfileGroups
{
    public static UnitProfileGroups Empty { get; } = new([]);

    public UnitProfileGroups(IReadOnlyList<ProfileGroup> groups)
    {
        Groups = groups;
    }

    public IReadOnlyList<ProfileGroup> Groups { get; }

    public bool IsEmpty => Groups.Count == 0;

    /// <summary>Every option of every group, with its group, in payload order.</summary>
    public IEnumerable<GroupedOption> AllOptions() =>
        Groups.SelectMany(group => group.Options.Select(option => new GroupedOption(group, option)));

    /// <summary>
    /// Finds an option by group and option id. When several groups share the id, the first group that
    /// actually contains the option wins.
    /// </summary>
    public ProfileOption? FindOption(int groupId, int optionId)
    {
        foreach (var group in Groups)
        {
            if (group.Id != groupId)
            {
                continue;
            }

            foreach (var option in group.Options)
            {
                if (option.Id == optionId)
                {
                    return option;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// The option's own entries of one kind followed by those of every option it includes, recursively.
    /// Each included option is visited once per call, so include cycles terminate. The starting option is
    /// not marked as visited, so an include that points back at it collects its entries one more time.
    /// </summary>
    public IReadOnlyList<ProfileEntry> GetEntriesWithIncludes(ProfileOption option, ProfileEntryKind kind)
    {
        var collected = new List<ProfileEntry>();
        CollectEntriesWithIncludes(option, kind, collected, new HashSet<IncludeReference>(), includeReference: null);
        return collected;
    }

    /// <summary>
    /// True when the group fields peripherals: its profiles declare peripherals, or any option grants
    /// peripherals itself or through an include.
    /// </summary>
    public bool IsControllerGroup(ProfileGroup group)
    {
        if (ControllerPeripheralIds(group).Count > 0)
        {
            return true;
        }

        return group.Options.Any(option => GetEntriesWithIncludes(option, ProfileEntryKind.Peripheral).Count > 0);
    }

    /// <summary>
    /// The peripherals to show for an option: its own (and included) peripherals, limited to the ids the
    /// controller profile declares when it declares any; falling back to the controller's own peripherals.
    /// </summary>
    public IReadOnlyList<ProfileEntry> GetDisplayPeripheralEntries(ProfileGroup group, ProfileOption option)
    {
        var allowedIds = ControllerPeripheralIds(group);
        var optionEntries = GetEntriesWithIncludes(option, ProfileEntryKind.Peripheral);
        IReadOnlyList<ProfileEntry> filtered = allowedIds.Count == 0
            ? optionEntries
            : optionEntries.Where(entry => entry.Id is int id && allowedIds.Contains(id)).ToList();

        return filtered.Count > 0 ? filtered : group.ControllerPeripheralEntries;
    }

    /// <summary>
    /// The option cost with duplicated peripherals collapsed for display: when an option fields more
    /// minis than peripherals and more than one peripheral is shown, every peripheral beyond the first is
    /// deducted at the per-model price. Costs that are not whole numbers, not positive, or not evenly
    /// divisible by the mini count are returned unchanged.
    /// </summary>
    public string GetAdjustedCost(ProfileGroup group, ProfileOption option)
    {
        var baseCostText = option.Cost;
        if (!int.TryParse(baseCostText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var baseCost))
        {
            return baseCostText;
        }

        var totalPeripheralCount = GetDisplayPeripheralEntries(group, option).Sum(entry => entry.Quantity);
        if (totalPeripheralCount <= 1)
        {
            return baseCostText;
        }

        var minis = option.Minis;
        if (minis <= 1 || minis <= totalPeripheralCount)
        {
            return baseCostText;
        }

        if (baseCost <= 0 || baseCost % minis != 0)
        {
            return baseCostText;
        }

        var removedPeripheralCount = totalPeripheralCount - 1;
        var perModelCost = baseCost / minis;
        var adjustedCost = Math.Max(0, baseCost - (removedPeripheralCount * perModelCost));
        return adjustedCost.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary><see cref="GetAdjustedCost"/> as a number.</summary>
    public int GetAdjustedCostValue(ProfileGroup group, ProfileOption option) =>
        ProfileValues.ParseCost(GetAdjustedCost(group, option));

    /// <summary>True when any option of the unit makes the trooper a Lieutenant.</summary>
    public bool HasLieutenantOption(IReadOnlyDictionary<int, string> skillNames) =>
        AllOptions().Any(x => x.Option.IsLieutenant(skillNames));

    /// <summary>
    /// The statline of a peripheral fielded by this unit, matched by name ignoring case and punctuation:
    /// a profile with that name (or any profile of a group with that ISC), else the first profile with stats
    /// in a group that has an option with that name.
    /// </summary>
    public UnitProfile? FindPeripheralProfile(string peripheralName)
    {
        var expected = NormalizeComparisonToken(peripheralName);
        if (string.IsNullOrWhiteSpace(expected))
        {
            return null;
        }

        foreach (var group in Groups)
        {
            var normalizedGroupIsc = NormalizeComparisonToken(group.Isc);
            foreach (var profile in group.Profiles)
            {
                if (NormalizeComparisonToken(profile.Name) == expected || normalizedGroupIsc == expected)
                {
                    return profile;
                }
            }

            foreach (var option in group.Options)
            {
                if (NormalizeComparisonToken(option.Name) != expected)
                {
                    continue;
                }

                var profileWithStats = group.Profiles.FirstOrDefault(profile => profile.HasStatFields);
                if (profileWithStats is not null)
                {
                    return profileWithStats;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// The first peripheral (name and statline) of the first option visible to the company: controller groups
    /// only when the unit has any, no SWC cost, and Lieutenant options only when the list is restricted
    /// to Lieutenants (unless the Lieutenant role is forced).
    /// </summary>
    public NamedProfile? FindFirstVisiblePeripheral(
        IReadOnlyDictionary<int, string> peripheralNames,
        IReadOnlyDictionary<int, string> skillNames,
        bool forceLieutenant,
        bool lieutenantOnlyUnits)
    {
        var hasControllerGroups = Groups.Any(IsControllerGroup);
        foreach (var group in Groups)
        {
            if (hasControllerGroups && !IsControllerGroup(group))
            {
                continue;
            }

            foreach (var option in group.Options)
            {
                if (option.CostsSwc)
                {
                    continue;
                }

                if (!forceLieutenant && lieutenantOnlyUnits && !option.IsLieutenant(skillNames))
                {
                    continue;
                }

                foreach (var entry in GetDisplayPeripheralEntries(group, option))
                {
                    if (entry.Id is not int peripheralId)
                    {
                        continue;
                    }

                    var name = peripheralNames.TryGetValue(peripheralId, out var resolvedName)
                        ? resolvedName
                        : peripheralId.ToString(CultureInfo.InvariantCulture);
                    if (FindPeripheralProfile(name) is { } profile)
                    {
                        return new NamedProfile(name, profile);
                    }
                }
            }
        }

        return null;
    }

    private static string NormalizeComparisonToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return System.Text.RegularExpressions.Regex
            .Replace(value, "[^a-z0-9]", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            .ToLowerInvariant();
    }

    private static HashSet<int> ControllerPeripheralIds(ProfileGroup group)
    {
        var ids = new HashSet<int>();
        foreach (var entry in group.ControllerPeripheralEntries)
        {
            if (entry.Id is int id)
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    private void CollectEntriesWithIncludes(
        ProfileOption option,
        ProfileEntryKind kind,
        List<ProfileEntry> target,
        HashSet<IncludeReference> visited,
        IncludeReference? includeReference)
    {
        if (includeReference is not null && !visited.Add(includeReference))
        {
            return;
        }

        target.AddRange(option.Entries(kind));

        foreach (var include in option.Includes)
        {
            var includedOption = FindOption(include.GroupId, include.OptionId);
            if (includedOption is not null)
            {
                CollectEntriesWithIncludes(includedOption, kind, target, visited, include);
            }
        }
    }
}
