using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Services.ArmyData;

namespace InfinityMercsApp.Services.CompanySelection.Units;

/// <summary>The choices a selection screen's unit filter offers, from the Mercs units of its source factions.</summary>
public sealed class UnitFilterOptionsCatalog(IArmyDataService armyData)
{
    /// <summary>The filter's points slider never ends below this.</summary>
    public const int MinimumMaxPoints = 200;

    /// <summary>
    /// Every classification, characteristic, skill, equipment, weapon and ammo the factions' units can field
    /// without SWC, with the slider running to <paramref name="maxPoints"/>.
    /// </summary>
    public UnitFilterPopupOptions For(IReadOnlyCollection<int> factionIds, int maxPoints, CancellationToken cancellationToken = default)
    {
        var catalogs = new List<ArmyFilterCatalog>();
        foreach (var factionId in factionIds.Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (armyData.GetFactionSnapshot(factionId, cancellationToken)?.Filters is { } filters)
            {
                catalogs.Add(filters);
            }
        }

        var typeLookup = UnitFilterLookups.MergeNames(catalogs.Select(c => c.Names(ArmyFilterSection.Type)));
        var lookups = UnitFilterLookups.Merge(catalogs);
        var classification = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var names = UnitFilterOptionSets.IgnoringCase();
        foreach (var entry in armyData.GetMergedMercsArmyList(factionIds.Distinct().ToList(), cancellationToken))
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

        return new UnitFilterPopupOptions
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
    }

    /// <summary>The slider's top for a points limit: the limit, but at least <see cref="MinimumMaxPoints"/>.</summary>
    public static int MaxPointsFor(string pointsLimit) =>
        int.TryParse(pointsLimit, out var parsed) ? Math.Max(parsed, MinimumMaxPoints) : MinimumMaxPoints;

    /// <summary>A copy of <paramref name="options"/> with the slider running to <paramref name="maxPoints"/>; no choices when there are none yet.</summary>
    public static UnitFilterPopupOptions WithMaxPoints(UnitFilterPopupOptions? options, int maxPoints) => options is null
        ? new UnitFilterPopupOptions { MinPoints = 0, MaxPoints = maxPoints }
        : new UnitFilterPopupOptions
        {
            Classification = [.. options.Classification],
            Characteristics = [.. options.Characteristics],
            Skills = [.. options.Skills],
            Equipment = [.. options.Equipment],
            Weapons = [.. options.Weapons],
            Ammo = [.. options.Ammo],
            MinPoints = options.MinPoints,
            MaxPoints = maxPoints
        };
}
