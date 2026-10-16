using System.Collections.ObjectModel;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Services.CompanySelection.Units;
using ArmyFactionRecord = InfinityMercsApp.Domain.Models.Army.Faction;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmySpecopsUnitRecord = InfinityMercsApp.Domain.Models.Army.SpecOps.SpecopsUnit;
using InfinityMercsApp.ViewModels.Company.Selection.Items;

namespace InfinityMercsApp.ViewModels.Company.Selection.Workflows;

internal static class CompanySelectionUnitLoadWorkflow
{
    internal static async Task<(Dictionary<string, ArmyUnitSelectionItem> UnitsByKey, Dictionary<string, MergedFireteam> TeamsByName)> BuildMergedUnitsAndTeamsAsync<TFaction>(
        IReadOnlyCollection<TFaction> factions,
        Func<TFaction, int> readFactionId,
        Func<int, CancellationToken, IReadOnlyList<ArmyResumeRecord>> getResumeByFaction,
        Func<int, CancellationToken, IReadOnlyList<ArmySpecopsUnitRecord>> getSpecopsByFaction,
        Func<int, CancellationToken, ArmyFactionRecord?> getFactionSnapshot,
        Func<int, IReadOnlyList<ArmyResumeRecord>, CancellationToken, Task> cacheUnitLogosAsync,
        Func<int, ArmyResumeRecord, IReadOnlyDictionary<int, string>, IReadOnlyDictionary<int, string>, ArmyUnitSelectionItem> createResumeUnit,
        Func<int, ArmySpecopsUnitRecord, IReadOnlyDictionary<int, ArmyResumeRecord>, IReadOnlyList<ArmyResumeRecord>, IReadOnlyDictionary<int, string>, IReadOnlyDictionary<int, string>, ArmyUnitSelectionItem> createSpecopsUnit,
        CancellationToken cancellationToken)
    {
        var mergedUnits = new Dictionary<string, ArmyUnitSelectionItem>(StringComparer.OrdinalIgnoreCase);
        var mergedTeams = new Dictionary<string, MergedFireteam>(StringComparer.OrdinalIgnoreCase);

        foreach (var faction in factions)
        {
            var factionId = readFactionId(faction);
            var units = getResumeByFaction(factionId, cancellationToken);
            var resumeByUnitId = units
                .GroupBy(x => x.UnitId)
                .ToDictionary(x => x.Key, x => x.First());
            var specopsUnits = getSpecopsByFaction(factionId, cancellationToken);
            var snapshot = getFactionSnapshot(factionId, cancellationToken);
            var typeLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.Filters, ArmyFilterSection.Type);
            var categoryLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.Filters, ArmyFilterSection.Category);
            MergedFireteam.MergeChart(snapshot?.FireteamChart, mergedTeams);

            await cacheUnitLogosAsync(factionId, units, cancellationToken);

            foreach (var unit in units)
            {
                var key = unit.Name.Trim();
                if (string.IsNullOrWhiteSpace(key) || mergedUnits.ContainsKey(key))
                {
                    continue;
                }

                mergedUnits[key] = createResumeUnit(factionId, unit, typeLookup, categoryLookup);
            }

            foreach (var specopsUnit in specopsUnits.OrderBy(x => x.EntryOrder))
            {
                var baseName = string.IsNullOrWhiteSpace(specopsUnit.Name)
                    ? units.FirstOrDefault(x => x.UnitId == specopsUnit.UnitId)?.Name ?? $"Unit {specopsUnit.UnitId}"
                    : specopsUnit.Name.Trim();
                var key = $"{baseName} - Spec Ops";
                if (string.IsNullOrWhiteSpace(key) || mergedUnits.ContainsKey(key))
                {
                    continue;
                }

                mergedUnits[key] = createSpecopsUnit(
                    factionId,
                    specopsUnit,
                    resumeByUnitId,
                    units,
                    typeLookup,
                    categoryLookup);
            }
        }

        return (mergedUnits, mergedTeams);
    }

    internal static void PopulateUnitsCollection(
        ICollection<ArmyUnitSelectionItem> unitsTarget,
        IEnumerable<ArmyUnitSelectionItem> mergedUnits)
    {
        foreach (var unit in ArmyUnitSort.OrderByUnitTypeAndName(mergedUnits, x => x.Type, x => x.Name))
        {
            unitsTarget.Add(unit);
        }
    }

    internal static Dictionary<string, MergedUnitLimit> BuildWildcardUnitLimits(
        IEnumerable<MergedFireteam> mergedTeams,
        IEnumerable<ArmyUnitSelectionItem> mergedUnits)
    {
        var wildcardUnitLimits = new Dictionary<string, MergedUnitLimit>(StringComparer.OrdinalIgnoreCase);
        foreach (var team in mergedTeams)
        {
            var isWildcardTeam = FireteamMatching.IsWildcardTeamName(team.Name);
            var nonCharacterUnitLimits = CompanyTeamProfilesWorkflow.FilterCharacterUnitLimits(
                team.UnitLimits,
                mergedUnits,
                x => x.IsCharacter);
            foreach (var entry in nonCharacterUnitLimits)
            {
                var unitName = entry.Key;
                var value = entry.Value;
                if (!isWildcardTeam && !FireteamMatching.IsWildcardEntry(unitName, value.Slug))
                {
                    continue;
                }

                wildcardUnitLimits[unitName] = wildcardUnitLimits.TryGetValue(unitName, out var existing)
                    ? existing.MergeWith(value)
                    : value;
            }
        }

        return wildcardUnitLimits;
    }

    internal static void AppendWildcardTeamEntry(
        Dictionary<string, MergedUnitLimit> wildcardUnitLimits,
        IEnumerable<ArmyUnitSelectionItem> mergedUnits,
        ICollection<ArmyTeamListItem> teamEntries,
        Func<string, string, string, string?, IEnumerable<ArmyUnitSelectionItem>, ArmyTeamUnitLimitItem> buildTeamUnitLimitItem,
        Func<string, string, bool, bool, ObservableCollection<ArmyTeamUnitLimitItem>, ArmyTeamListItem> createTeam)
    {
        if (wildcardUnitLimits.Count == 0)
        {
            return;
        }

        var wildcardAllowedProfiles = wildcardUnitLimits
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => buildTeamUnitLimitItem(
                x.Key,
                x.Value.MinText,
                x.Value.MaxText,
                x.Value.Slug,
                mergedUnits))
            .Where(x => !x.IsCharacter)
            .ToList();

        if (wildcardAllowedProfiles.Count == 0)
        {
            return;
        }

        teamEntries.Add(createTeam(
            MergedUnitLimit.WildcardsTeamName,
            string.Empty,
            true,
            true,
            new ObservableCollection<ArmyTeamUnitLimitItem>(wildcardAllowedProfiles)));
    }

    internal static void BuildTeamEntriesFromMerged(
        IReadOnlyDictionary<string, ArmyUnitSelectionItem> unitsByKey,
        IEnumerable<MergedFireteam> mergedTeams,
        ICollection<ArmyTeamListItem> teamEntries,
        Func<MergedFireteam, bool> includeTeam,
        Func<MergedFireteam, int> readTeamCount,
        Func<MergedFireteam, string> buildTeamCountText,
        Func<string, string, string, string?, IEnumerable<ArmyUnitSelectionItem>, ArmyTeamUnitLimitItem> buildTeamUnitLimitItem,
        Func<string, string, bool, bool, ObservableCollection<ArmyTeamUnitLimitItem>, ArmyTeamListItem> createTeam)
    {
        foreach (var team in mergedTeams
                     .Where(includeTeam)
                     .Where(x => readTeamCount(x) > 0)
                     .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var nonCharacterUnitLimits = CompanyTeamProfilesWorkflow.FilterCharacterUnitLimits(
                team.UnitLimits,
                unitsByKey.Values,
                x => x.IsCharacter);
            var nonCharacterNonWildcardUnitLimits = CompanyTeamProfilesWorkflow.FilterWildcardUnitLimits(nonCharacterUnitLimits);
            var allowedProfiles = CompanyTeamProfilesWorkflow.BuildAllowedTeamProfiles(
                nonCharacterNonWildcardUnitLimits,
                unitsByKey.Values,
                buildTeamUnitLimitItem);
            if (allowedProfiles.Count == 0)
            {
                continue;
            }

            teamEntries.Add(createTeam(
                team.Name,
                buildTeamCountText(team),
                false,
                true,
                new ObservableCollection<ArmyTeamUnitLimitItem>(allowedProfiles)));
        }

        var wildcardUnitLimits = BuildWildcardUnitLimits(
            mergedTeams,
            unitsByKey.Values);
        AppendWildcardTeamEntry(
            wildcardUnitLimits,
            unitsByKey.Values,
            teamEntries,
            buildTeamUnitLimitItem,
            createTeam);
    }
}
