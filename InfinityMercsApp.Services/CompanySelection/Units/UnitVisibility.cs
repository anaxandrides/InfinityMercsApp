using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Teams;

namespace InfinityMercsApp.Services.CompanySelection.Units;

/// <summary>Whether a selection screen's units show under its filter and the points left.</summary>
public sealed class UnitVisibility(IArmyDataService armyData)
{
    /// <summary>The filter lookups and type names of the source factions.</summary>
    public CompanyUnitVisibilityLookupContext Lookups(IEnumerable<int> factionIds, CancellationToken cancellationToken = default)
    {
        var context = new CompanyUnitVisibilityLookupContext();
        foreach (var factionId in factionIds)
        {
            var filters = armyData.GetFactionSnapshot(factionId, cancellationToken)?.Filters;
            context.LookupsByFactionId[factionId] = UnitFilterLookups.From(filters);
            context.TypeByFactionId[factionId] = (filters ?? ArmyFilterCatalog.Empty).Names(ArmyFilterSection.Type);
        }

        return context;
    }

    /// <summary>The unit's profiles; a Spec-Ops unit's are only its own, not its SpecBot's.</summary>
    public UnitProfileGroups ProfileGroupsOf(
        CompanyUnitVisibilityLookupContext context, ISelectionUnit unit, CancellationToken cancellationToken = default)
    {
        var profileGroups = armyData.GetUnit(unit.SourceFactionId, unit.Id, cancellationToken)?.ProfileGroups ?? UnitProfileGroups.Empty;
        return unit.IsSpecOps ? profileGroups.FirstGroupOnly() : profileGroups;
    }

    /// <summary>
    /// Whether the unit matches the filter's classification and has a profile without SWC that fits the filter
    /// and <paramref name="pointsRemaining"/> (and is a lieutenant, with <paramref name="lieutenantOnly"/>).
    /// </summary>
    public bool Shows(
        CompanyUnitVisibilityLookupContext context,
        ISelectionUnit unit,
        UnitFilterCriteria filter,
        bool lieutenantOnly,
        int pointsRemaining,
        CancellationToken cancellationToken = default)
    {
        if (!context.LookupsByFactionId.TryGetValue(unit.SourceFactionId, out var lookups))
        {
            return false;
        }

        var typeLookup = context.TypeByFactionId.TryGetValue(unit.SourceFactionId, out var types) ? types : new Dictionary<int, string>();
        return UnitOptionFilter.MatchesClassification(filter, unit.Type, typeLookup) &&
               UnitOptionFilter.HasVisibleOption(
                   ProfileGroupsOf(context, unit, cancellationToken),
                   lookups,
                   filter,
                   requireLieutenant: lieutenantOnly && !unit.IsSpecOps,
                   requireZeroSwc: true,
                   maxCost: pointsRemaining);
    }
}
