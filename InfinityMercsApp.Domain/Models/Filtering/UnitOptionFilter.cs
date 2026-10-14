using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Models.Army.Profiles;

namespace InfinityMercsApp.Domain.Models.Filtering;

/// <summary>
/// Which options of a unit are visible to a company type and match the unit list filters.
/// </summary>
public static class UnitOptionFilter
{
    private static readonly IReadOnlyList<ProfileEntryKind> AmmoKinds = [ProfileEntryKind.Ammunition, ProfileEntryKind.Ammo];

    /// <summary>
    /// True when at least one option passes the visibility rules (optional name predicate, Lieutenant only,
    /// no positive SWC, cost cap) and matches every non-classification term and the points range.
    /// </summary>
    public static bool HasVisibleOption(
        UnitProfileGroups groups,
        UnitFilterLookups lookups,
        UnitFilterCriteria criteria,
        bool requireLieutenant,
        bool requireZeroSwc,
        int? maxCost = null,
        Func<string?, bool>? optionNamePredicate = null)
    {
        var query = criteria.ToQuery();
        foreach (var (group, option) in groups.AllOptions())
        {
            if (optionNamePredicate is not null && !optionNamePredicate(option.Name))
            {
                continue;
            }

            if (!PassesVisibility(groups, group, option, lookups, requireLieutenant, requireZeroSwc, maxCost, out var cost))
            {
                continue;
            }

            if (query.MinPoints.HasValue && cost < query.MinPoints.Value)
            {
                continue;
            }

            if (query.MaxPoints.HasValue && cost > query.MaxPoints.Value)
            {
                continue;
            }

            if (MatchesTerms(groups, group, option, lookups, query))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Adds the names offered by every visible option (including included options) and, optionally, by the
    /// profiles of groups that have a visible option.
    /// </summary>
    public static void CollectFilterOptions(
        UnitProfileGroups groups,
        UnitFilterLookups lookups,
        bool requireLieutenant,
        bool requireZeroSwc,
        int? maxCost,
        bool includeProfileValues,
        UnitFilterOptionSets target)
    {
        foreach (var group in groups.Groups)
        {
            var groupHasVisibleOption = false;
            foreach (var option in group.Options)
            {
                if (!PassesVisibility(groups, group, option, lookups, requireLieutenant, requireZeroSwc, maxCost, out _))
                {
                    continue;
                }

                groupHasVisibleOption = true;
                AddNames(groups.GetEntriesWithIncludes(option, ProfileEntryKind.Chars), lookups.Chars, target.Characteristics);
                AddNames(groups.GetEntriesWithIncludes(option, ProfileEntryKind.Skills), lookups.Skills, target.Skills);
                AddNames(groups.GetEntriesWithIncludes(option, ProfileEntryKind.Equip), lookups.Equip, target.Equipment);
                AddNames(groups.GetEntriesWithIncludes(option, ProfileEntryKind.Weapons), lookups.Weapons, target.Weapons);
                AddNames(groups.GetEntriesWithIncludes(option, ProfileEntryKind.Ammunition), lookups.Ammo, target.Ammo);
                AddNames(groups.GetEntriesWithIncludes(option, ProfileEntryKind.Ammo), lookups.Ammo, target.Ammo);
            }

            if (!groupHasVisibleOption || !includeProfileValues)
            {
                continue;
            }

            foreach (var profile in group.Profiles)
            {
                AddNames(profile.Chars, lookups.Chars, target.Characteristics);
                AddNames(profile.Skills, lookups.Skills, target.Skills);
                AddNames(profile.Equip, lookups.Equip, target.Equipment);
                AddNames(profile.Weapons, lookups.Weapons, target.Weapons);
                AddNames(profile.Ammunition, lookups.Ammo, target.Ammo);
                AddNames(profile.Ammo, lookups.Ammo, target.Ammo);
            }
        }
    }

    /// <summary>
    /// True when no classification is filtered, or the unit's type name matches the classification term.
    /// </summary>
    public static bool MatchesClassification(
        UnitFilterCriteria criteria,
        int? unitType,
        IReadOnlyDictionary<int, string> typeLookup)
    {
        var classificationTerm = criteria.ToQuery().GetTerm(UnitFilterField.Classification);
        if (classificationTerm is null || classificationTerm.Values.Count == 0)
        {
            return true;
        }

        if (!unitType.HasValue || typeLookup.Count == 0 || !typeLookup.TryGetValue(unitType.Value, out var typeName))
        {
            return false;
        }

        return classificationTerm.MatchMode == UnitFilterMatchMode.All
            ? classificationTerm.Values.All(value => string.Equals(typeName, value, StringComparison.OrdinalIgnoreCase))
            : classificationTerm.Values.Any(value => string.Equals(typeName, value, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The per-model cost of a peripheral fielded by this unit: the first priced option of the first group
    /// whose ISC, profile name or option name matches <paramref name="peripheralName"/> (ignoring case and
    /// punctuation), divided by its mini count.
    /// </summary>
    public static bool TryGetPeripheralUnitCost(UnitProfileGroups groups, string peripheralName, out int peripheralUnitCost)
    {
        peripheralUnitCost = 0;
        var expected = NormalizeComparisonToken(peripheralName);
        if (string.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        foreach (var group in groups.Groups)
        {
            var groupMatch = NormalizeComparisonToken(group.Isc) == expected ||
                             group.Profiles.Any(profile => NormalizeComparisonToken(profile.Name) == expected) ||
                             group.Options.Any(option => NormalizeComparisonToken(option.Name) == expected);
            if (!groupMatch)
            {
                continue;
            }

            foreach (var option in group.Options)
            {
                var optionCost = option.CostValue;
                if (optionCost <= 0)
                {
                    continue;
                }

                var minis = Math.Max(1, option.Minis);
                peripheralUnitCost = Math.Max(1, optionCost / minis);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The cost of an option shown with a single peripheral instead of <paramref name="peripheralCount"/>:
    /// the cost less one per-model peripheral cost for each peripheral dropped. Unchanged when either the
    /// cost or the peripheral's price is unknown or zero.
    /// </summary>
    public static string SinglePeripheralCost(UnitProfileGroups groups, string cost, string peripheralName, int peripheralCount)
    {
        var baseCost = ProfileValues.ParseCost(cost);
        if (peripheralCount <= 1 ||
            baseCost <= 0 ||
            !TryGetPeripheralUnitCost(groups, peripheralName, out var peripheralCost) ||
            peripheralCost <= 0)
        {
            return cost;
        }

        return Math.Max(0, baseCost - ((peripheralCount - 1) * peripheralCost))
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static bool PassesVisibility(
        UnitProfileGroups groups,
        ProfileGroup group,
        ProfileOption option,
        UnitFilterLookups lookups,
        bool requireLieutenant,
        bool requireZeroSwc,
        int? maxCost,
        out int cost)
    {
        cost = 0;
        if (requireLieutenant && !option.IsLieutenant(lookups.Skills))
        {
            return false;
        }

        if (requireZeroSwc && option.HasPositiveSwc)
        {
            return false;
        }

        cost = groups.GetAdjustedCostValue(group, option);
        return !(maxCost.HasValue && cost > maxCost.Value);
    }

    private static bool MatchesTerms(
        UnitProfileGroups groups,
        ProfileGroup group,
        ProfileOption option,
        UnitFilterLookups lookups,
        UnitFilterQuery query)
    {
        foreach (var term in query.Terms)
        {
            if (term.Field == UnitFilterField.Classification || term.Values.Count == 0)
            {
                continue;
            }

            Func<string, bool> containsValue = term.Field switch
            {
                UnitFilterField.Characteristics => value => OptionOrGroupContains(groups, group, option, ProfileEntryKind.Chars, lookups.Chars, value),
                UnitFilterField.Skills => value => OptionOrGroupContains(groups, group, option, ProfileEntryKind.Skills, lookups.Skills, value),
                UnitFilterField.Equipment => value => OptionOrGroupContains(groups, group, option, ProfileEntryKind.Equip, lookups.Equip, value),
                UnitFilterField.Weapons => value => OptionOrGroupContains(groups, group, option, ProfileEntryKind.Weapons, lookups.Weapons, value),
                UnitFilterField.Ammo => value => AmmoKinds.Any(kind => OptionContains(groups, option, kind, lookups.Ammo, value)),
                _ => _ => true
            };

            var matches = term.MatchMode == UnitFilterMatchMode.All
                ? term.Values.All(containsValue)
                : term.Values.Any(containsValue);
            if (!matches)
            {
                return false;
            }
        }

        return true;
    }

    private static bool OptionOrGroupContains(
        UnitProfileGroups groups,
        ProfileGroup group,
        ProfileOption option,
        ProfileEntryKind kind,
        IReadOnlyDictionary<int, string> lookup,
        string expectedName)
    {
        if (OptionContains(groups, option, kind, lookup, expectedName))
        {
            return true;
        }

        if (lookup.Count == 0 || string.IsNullOrWhiteSpace(expectedName))
        {
            return false;
        }

        return group.Profiles.Any(profile => ContainsName(profile.Entries(kind), lookup, expectedName));
    }

    private static bool OptionContains(
        UnitProfileGroups groups,
        ProfileOption option,
        ProfileEntryKind kind,
        IReadOnlyDictionary<int, string> lookup,
        string expectedName)
    {
        if (lookup.Count == 0 || string.IsNullOrWhiteSpace(expectedName))
        {
            return false;
        }

        return ContainsName(groups.GetEntriesWithIncludes(option, kind), lookup, expectedName);
    }

    private static bool ContainsName(IEnumerable<ProfileEntry> entries, IReadOnlyDictionary<int, string> lookup, string expectedName) =>
        entries.Any(entry =>
            entry.Id is int id &&
            lookup.TryGetValue(id, out var name) &&
            string.Equals(name, expectedName, StringComparison.OrdinalIgnoreCase));

    private static void AddNames(IEnumerable<ProfileEntry> entries, IReadOnlyDictionary<int, string> lookup, ISet<string> target)
    {
        foreach (var entry in entries)
        {
            if (entry.Id is int id && lookup.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name))
            {
                target.Add(name.Trim());
            }
        }
    }

    private static string NormalizeComparisonToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return Regex.Replace(value, "[^a-z0-9]", string.Empty, RegexOptions.IgnoreCase).ToLowerInvariant();
    }
}
