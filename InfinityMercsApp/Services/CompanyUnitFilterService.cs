using System.Text.Json;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Infrastructure.Json;

namespace InfinityMercsApp.Services;

/// <summary>
/// Unit-list filtering entry points for pages and workflows that still hold raw <c>ProfileGroupsJson</c>.
/// The rules live in <see cref="UnitOptionFilter"/>; this class only reads the JSON through the shared
/// <see cref="ArmyJsonReader"/> (which caches parsed units).
/// </summary>
public static class CompanyUnitFilterService
{
    private static readonly IReadOnlyDictionary<int, string> EmptyLookup = new Dictionary<int, string>();

    private static IArmyJsonReader Reader => ArmyJsonReader.Shared;

    public static void AddFilterOptionsFromVisibleProfilesAndOptions(
        string profileGroupsJson,
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
            Reader.ReadProfileGroups(profileGroupsJson),
            new UnitFilterLookups(charsLookup, skillsLookup, equipLookup, weaponsLookup, ammoLookup),
            requireLieutenant,
            requireZeroSwc,
            maxCost,
            includeProfileValues,
            new UnitFilterOptionSets(characteristics, skills, equipment, weapons, ammo));
    }

    public static bool UnitHasVisibleOptionWithFilter(
        string? profileGroupsJson,
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
            Reader.ReadProfileGroups(profileGroupsJson),
            new UnitFilterLookups(charsLookup, skillsLookup, equipLookup, weaponsLookup, ammoLookup),
            criteria,
            requireLieutenant,
            requireZeroSwc,
            maxCost,
            optionNamePredicate);
    }

    public static bool UnitHasVisibleOption(
        string? profileGroupsJson,
        IReadOnlyDictionary<int, string> skillsLookup,
        bool requireLieutenant,
        bool requireZeroSwc,
        int? maxCost = null)
    {
        return UnitHasVisibleOptionWithFilter(
            profileGroupsJson,
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
        string? profileGroupsJson,
        IReadOnlyDictionary<int, string> skillsLookup)
    {
        return Reader.ReadProfileGroups(profileGroupsJson).HasLieutenantOption(skillsLookup);
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

    /// <summary>
    /// Transitional overload for unit-detail pages that still hold a parsed <see cref="JsonElement"/>;
    /// removed once those pages read <see cref="UnitProfileGroups"/> directly.
    /// </summary>
    public static bool TryGetPeripheralUnitCost(JsonElement profileGroupsRoot, string peripheralName, out int peripheralUnitCost)
    {
        peripheralUnitCost = 0;
        if (profileGroupsRoot.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        return TryGetPeripheralUnitCost(Reader.ReadProfileGroups(profileGroupsRoot.GetRawText()), peripheralName, out peripheralUnitCost);
    }
}
