using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Domain.Models.Filtering;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using MercsArmyListEntry = InfinityMercsApp.Domain.Models.Army.MercsArmyListEntry;

namespace InfinityMercsApp.Views.Common;

internal static class CompanySelectionUnitFilterOptionsService
{
    internal static UnitFilterCriteria ApplyCriteriaFromPopup(
        UnitFilterCriteria? criteria,
        Action<bool> setLieutenantOnlyUnits,
        Action<bool> setTeamsView)
    {
        var resolved = criteria ?? UnitFilterCriteria.None;
        if (criteria is not null)
        {
            setLieutenantOnlyUnits(criteria.LieutenantOnlyUnits);
            setTeamsView(criteria.TeamsView);
            resolved = new UnitFilterCriteria
            {
                Terms = criteria.Terms,
                MinPoints = criteria.MinPoints,
                MaxPoints = criteria.MaxPoints,
                LieutenantOnlyUnits = criteria.LieutenantOnlyUnits,
                TeamsView = criteria.TeamsView
            };
        }

        return resolved;
    }

    internal static int ResolveFilterPopupMaxPoints(string selectedStartSeasonPoints)
    {
        return int.TryParse(selectedStartSeasonPoints, out var parsedMaxPoints)
            ? Math.Max(parsedMaxPoints, 200)
            : 200;
    }

    internal static UnitFilterPopupOptions ClonePopupOptionsForCurrentPoints(UnitFilterPopupOptions source, int maxPoints)
    {
        return new UnitFilterPopupOptions
        {
            Classification = [.. source.Classification],
            Characteristics = [.. source.Characteristics],
            Skills = [.. source.Skills],
            Equipment = [.. source.Equipment],
            Weapons = [.. source.Weapons],
            Ammo = [.. source.Ammo],
            MinPoints = source.MinPoints,
            MaxPoints = maxPoints
        };
    }

    internal static UnitFilterPopupOptions GetPreparedPopupOptionsForCurrentPoints(
        UnitFilterPopupOptions? preparedOptions,
        int maxPoints)
    {
        if (preparedOptions is null)
        {
            return new UnitFilterPopupOptions
            {
                MinPoints = 0,
                MaxPoints = maxPoints
            };
        }

        return ClonePopupOptionsForCurrentPoints(preparedOptions, maxPoints);
    }

    internal static UnitFilterPopupOptions BuildUnitFilterPopupOptions<TFaction>(
        bool showRightSelectionBox,
        TFaction? leftSlotFaction,
        TFaction? rightSlotFaction,
        Func<TFaction, int> readFactionId,
        Func<int, CancellationToken, ArmyFilterCatalog?> getFiltersByFactionId,
        Func<IReadOnlyCollection<int>, CancellationToken, IReadOnlyList<MercsArmyListEntry>> getMergedMercsArmyList,
        int maxPoints,
        Action<UnitFilterPopupOptions> setPreparedOptions,
        Action<string>? log = null,
        CancellationToken cancellationToken = default)
        where TFaction : class
    {
        var classification = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var names = UnitFilterOptionSets.IgnoringCase();

        var sourceFactions = CompanyUnitDetailsShared.BuildUnitSourceFactions(
            showRightSelectionBox,
            leftSlotFaction,
            rightSlotFaction,
            readFactionId);
        var sourceFactionIds = sourceFactions
            .Select(readFactionId)
            .Distinct()
            .ToArray();
        var catalogs = new List<ArmyFilterCatalog>();
        foreach (var factionId in sourceFactionIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (getFiltersByFactionId(factionId, cancellationToken) is { } filters)
            {
                catalogs.Add(filters);
            }
        }

        var typeLookup = UnitFilterLookups.MergeNames(catalogs.Select(c => c.Names(ArmyFilterSection.Type)));
        var lookups = UnitFilterLookups.Merge(catalogs);

        var mergedMercsList = getMergedMercsArmyList(sourceFactionIds, cancellationToken);
        foreach (var entry in mergedMercsList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entry.Resume.Type is int typeId &&
                typeLookup.TryGetValue(typeId, out var typeName) &&
                !string.IsNullOrWhiteSpace(typeName))
            {
                classification.Add(typeName.Trim());
            }

            if (entry.ProfileGroups is not { IsEmpty: false })
            {
                continue;
            }

            UnitOptionFilter.CollectFilterOptions(
                entry.ProfileGroups,
                lookups,
                requireLieutenant: false,
                requireZeroSwc: true,
                maxCost: null,
                includeProfileValues: true,
                names);
        }

        var options = new UnitFilterPopupOptions
        {
            Classification = classification.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            Characteristics = UnitFilterOptionSets.Sorted(names.Characteristics),
            Skills = UnitFilterOptionSets.Sorted(names.Skills),
            Equipment = UnitFilterOptionSets.Sorted(names.Equipment),
            Weapons = UnitFilterOptionSets.Sorted(names.Weapons),
            Ammo = UnitFilterOptionSets.Sorted(names.Ammo),
            MinPoints = 0,
            MaxPoints = maxPoints
        };

        setPreparedOptions(options);
        log?.Invoke(
            $"CompanySelectionPage filter options: class={options.Classification.Count}, chars={options.Characteristics.Count}, skills={options.Skills.Count}, equip={options.Equipment.Count}, weapons={options.Weapons.Count}, ammo={options.Ammo.Count}.");
        return ClonePopupOptionsForCurrentPoints(options, maxPoints);
    }
}
