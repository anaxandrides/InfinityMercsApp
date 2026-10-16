using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Services.CompanySelection.Teams;
using InfinityMercsApp.Services.CompanySelection.Units;
using ArmyFactionRecord = InfinityMercsApp.Domain.Models.Army.Faction;
using ArmySpecopsUnitRecord = InfinityMercsApp.Domain.Models.Army.SpecOps.SpecopsUnit;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;
using InfinityMercsApp.ViewModels.Company.Selection.Items;

namespace InfinityMercsApp.Services.CompanySelection.Core;

internal static class CompanySelectionVisibilityWorkflow
{
    internal static int ComputePointsRemaining(string selectedStartSeasonPoints, string seasonPointsCapText)
    {
        var pointsLimit = int.TryParse(selectedStartSeasonPoints, out var parsedLimit) ? parsedLimit : 0;
        var currentPoints = int.TryParse(seasonPointsCapText, out var parsedPoints) ? parsedPoints : 0;
        return pointsLimit - currentPoints;
    }


    internal static CompanyUnitVisibilityLookupContext BuildUnitVisibilityLookupContext<TFaction>(
        IEnumerable<TFaction> factions,
        Func<TFaction, int> readFactionId,
        Func<int, CancellationToken, ArmyFactionRecord?> getFactionSnapshot,
        Func<int, CancellationToken, IReadOnlyList<ArmySpecopsUnitRecord>> getSpecopsUnitsByFaction,
        CancellationToken cancellationToken)
    {
        var context = new CompanyUnitVisibilityLookupContext();
        foreach (var faction in factions)
        {
            var factionId = readFactionId(faction);
            var snapshot = getFactionSnapshot(factionId, cancellationToken);
            context.LookupsByFactionId[factionId] = UnitFilterLookups.From(snapshot?.Filters);
            context.TypeByFactionId[factionId] = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.Filters, ArmyFilterSection.Type);

            var specopsUnits = getSpecopsUnitsByFaction(factionId, cancellationToken);
            context.SpecopsByFactionId[factionId] = specopsUnits
                .GroupBy(x => x.UnitId)
                .ToDictionary(x => x.Key, x => x.First());
        }

        return context;
    }

    internal static void ApplyUnitVisibility(
        IEnumerable<ArmyUnitSelectionItem> units,
        CompanyUnitVisibilityLookupContext lookupContext,
        UnitFilterCriteria activeUnitFilter,
        bool lieutenantOnlyUnits,
        int pointsRemaining,
        Func<int, int, CancellationToken, ArmyUnitRecord?> getUnitByFactionAndId,
        CancellationToken cancellationToken)
    {
        foreach (var unit in units)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!lookupContext.LookupsByFactionId.TryGetValue(unit.SourceFactionId, out var lookups))
            {
                unit.IsVisible = false;
                continue;
            }

            lookupContext.TypeByFactionId.TryGetValue(unit.SourceFactionId, out var typeLookup);

            if (!CompanyUnitDetailsShared.MatchesClassificationFilter(activeUnitFilter, unit.Type, typeLookup ?? new Dictionary<int, string>()))
            {
                unit.IsVisible = false;
                continue;
            }

            var unitRecord = getUnitByFactionAndId(unit.SourceFactionId, unit.Id, cancellationToken);
            var profileGroups = unitRecord?.ProfileGroups ?? UnitProfileGroups.Empty;
            if (lookupContext.SpecopsByFactionId.TryGetValue(unit.SourceFactionId, out var specopsUnitsById) &&
                specopsUnitsById.TryGetValue(unit.Id, out var specopsUnit))
            {
                if (unit.IsSpecOps || profileGroups.IsEmpty)
                {
                    profileGroups = specopsUnit.ProfileGroups;
                }
            }

            unit.IsVisible = UnitOptionFilter.HasVisibleOption(
                profileGroups,
                lookups,
                activeUnitFilter,
                requireLieutenant: lieutenantOnlyUnits && !unit.IsSpecOps,
                requireZeroSwc: true,
                maxCost: pointsRemaining);
        }
    }

    internal static void RefreshTeamEntryVisibility(
        IEnumerable<ArmyTeamListItem> teamEntries,
        IEnumerable<ArmyUnitSelectionItem> units)
    {
        var teamList = teamEntries as IReadOnlyList<ArmyTeamListItem> ?? teamEntries.ToList();
        var unitList = units as IReadOnlyList<ArmyUnitSelectionItem> ?? units.ToList();
        CompanyTeamVisibilityWorkflow.RefreshTeamEntryVisibility<ArmyTeamListItem, ArmyTeamUnitLimitItem, ArmyUnitSelectionItem>(
            teamList,
            unitList,
            team => team.AllowedProfiles,
            team => team.IsWildcardBucket,
            (team, value) => team.IsVisible = value,
            (team, value) => team.IsExpanded = value,
            allowed => allowed.IsCharacter,
            (allowed, value) => allowed.IsVisible = value,
            allowed => allowed.Name,
            allowed => allowed.Slug,
            allowed => allowed.ResolvedUnitId,
            allowed => allowed.ResolvedSourceFactionId,
            unit => unit.IsVisible,
            unit => unit.Id,
            unit => unit.SourceFactionId,
            unit => unit.Name,
            unit => unit.Slug);
    }
}
