using InfinityMercsApp.Domain.Models.Army.Profiles;

namespace InfinityMercsApp.Services;

/// <summary>
/// Unit-list filtering entry points for pages and workflows. The rules live in <see cref="UnitOptionFilter"/>.
/// </summary>
public static class CompanyUnitFilterService
{
    private static readonly IReadOnlyDictionary<int, string> EmptyLookup = new Dictionary<int, string>();

    public static void AddFilterOptionsFromVisibleProfilesAndOptions(
        UnitProfileGroups profileGroups,
        IReadOnlyDictionary<int, string> charsLookup,
        IReadOnlyDictionary<int, string> skillsLookup,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> weaponsLookup,
        IReadOnlyDictionary<int, string> ammoLookup,
        bool requireLieutenant,
        bool requireZeroSwc,
        int? maxCost,
        bool includeProfileValues,
        HashSet<string> characteristics,
        HashSet<string> skills,
        HashSet<string> equipment,
        HashSet<string> weapons,
        HashSet<string> ammo)
    {
        UnitOptionFilter.CollectFilterOptions(
            profileGroups,
            new UnitFilterLookups(charsLookup, skillsLookup, equipLookup, weaponsLookup, ammoLookup),
            requireLieutenant,
            requireZeroSwc,
            maxCost,
            includeProfileValues,
            new UnitFilterOptionSets(characteristics, skills, equipment, weapons, ammo));
    }

    public static bool UnitHasVisibleOptionWithFilter(
        UnitProfileGroups profileGroups,
        IReadOnlyDictionary<int, string> skillsLookup,
        IReadOnlyDictionary<int, string> charsLookup,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> weaponsLookup,
        IReadOnlyDictionary<int, string> ammoLookup,
        UnitFilterCriteria criteria,
        bool requireLieutenant,
        bool requireZeroSwc,
        int? maxCost = null,
        Func<string?, bool>? optionNamePredicate = null)
    {
        return UnitOptionFilter.HasVisibleOption(
            profileGroups,
            new UnitFilterLookups(charsLookup, skillsLookup, equipLookup, weaponsLookup, ammoLookup),
            criteria,
            requireLieutenant,
            requireZeroSwc,
            maxCost,
            optionNamePredicate);
    }

    public static bool UnitHasVisibleOption(
        UnitProfileGroups profileGroups,
        IReadOnlyDictionary<int, string> skillsLookup,
        bool requireLieutenant,
        bool requireZeroSwc,
        int? maxCost = null)
    {
        return UnitHasVisibleOptionWithFilter(
            profileGroups,
            skillsLookup,
            EmptyLookup,
            EmptyLookup,
            EmptyLookup,
            EmptyLookup,
            UnitFilterCriteria.None,
            requireLieutenant,
            requireZeroSwc,
            maxCost);
    }

    public static bool UnitHasLieutenantOption(
        UnitProfileGroups profileGroups,
        IReadOnlyDictionary<int, string> skillsLookup)
    {
        return profileGroups.HasLieutenantOption(skillsLookup);
    }

    public static bool MatchesClassificationFilter(
        UnitFilterCriteria criteria,
        int? unitType,
        IReadOnlyDictionary<int, string> typeLookup)
    {
        return UnitOptionFilter.MatchesClassification(criteria, unitType, typeLookup);
    }

    public static int ParseCostValue(string? cost) => ProfileValues.ParseCost(cost);

    public static bool TryGetPeripheralUnitCost(UnitProfileGroups profileGroups, string peripheralName, out int peripheralUnitCost)
    {
        return UnitOptionFilter.TryGetPeripheralUnitCost(profileGroups, peripheralName, out peripheralUnitCost);
    }
}
