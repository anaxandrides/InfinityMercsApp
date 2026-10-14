using InfinityMercsApp.Domain.Models.Filtering;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;

namespace InfinityMercsApp.Views.Common;

internal static class CohesiveCompanyTeamEligibilityWorkflow
{
    private sealed class EligibilitySourceUnit
    {
        public required int Id { get; init; }
        public required string? Name { get; init; }
        public required string? Slug { get; init; }
        public required int? Type { get; init; }
        public required bool IsCharacter { get; init; }
    }

    internal static List<string> EvaluateValidCoreFireteams(
        FireteamChart? fireteamChart,
        IReadOnlyList<ArmyResumeRecord> units,
        UnitFilterLookups lookups,
        IReadOnlyDictionary<int, string> typeLookup,
        UnitFilterCriteria activeUnitFilter,
        int maxCost,
                Func<ArmyResumeRecord, bool> isCharacterCategory,
        Func<int, ArmyUnitRecord?> getUnitById)
    {
        var sourceUnits = units.Select(unit => new EligibilitySourceUnit
            {
                Id = unit.UnitId,
                Name = unit.Name,
                Slug = unit.Slug,
                Type = unit.Type,
                IsCharacter = isCharacterCategory(unit)
            })
            .ToList();

        var teams = new Dictionary<string, MergedFireteam>(StringComparer.OrdinalIgnoreCase);
        MergedFireteam.MergeChart(fireteamChart, teams);

        var validCoreFireteams = new List<string>();
        foreach (var team in teams.Values
                     .Where(x => x.Core > 0)
                     .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var hasLieutenantProfileAfterFilters = false;
            foreach (var unitLimit in team.UnitLimits)
            {
                var matchedUnit = FireteamMatching.ResolveUnitForTeamEntry(
                    unitLimit.Key,
                    unitLimit.Value.Slug,
                    sourceUnits,
                    x => x.Name,
                    x => x.Slug);
                if (matchedUnit is null || matchedUnit.IsCharacter)
                {
                    continue;
                }

                if (!CompanyUnitDetailsShared.MatchesClassificationFilter(activeUnitFilter, matchedUnit.Type, typeLookup))
                {
                    continue;
                }

                var unitRecord = getUnitById(matchedUnit.Id);
                if (unitRecord?.ProfileGroups is not { IsEmpty: false })
                {
                    continue;
                }

                var requiresFtoProfile = FireteamMatching.IsFtoLabel(unitLimit.Key);
                var hasAnyVisibleProfile = UnitOptionFilter.HasVisibleOption(
                    unitRecord.ProfileGroups,
                    lookups,
                    activeUnitFilter,
                    requireLieutenant: false,
                    requireZeroSwc: true,
                    maxCost: maxCost,
                    optionNamePredicate: requiresFtoProfile ? FireteamMatching.IsFtoLabel : null);
                if (!hasAnyVisibleProfile)
                {
                    continue;
                }

                var hasVisibleLieutenantProfile = UnitOptionFilter.HasVisibleOption(
                    unitRecord.ProfileGroups,
                    lookups,
                    activeUnitFilter,
                    requireLieutenant: true,
                    requireZeroSwc: true,
                    maxCost: maxCost,
                    optionNamePredicate: requiresFtoProfile ? FireteamMatching.IsFtoLabel : null);
                if (!hasVisibleLieutenantProfile)
                {
                    continue;
                }

                hasLieutenantProfileAfterFilters = true;
                break;
            }

            if (hasLieutenantProfileAfterFilters)
            {
                validCoreFireteams.Add(team.Name);
            }
        }

        return validCoreFireteams
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>The cached valid CORE fireteam names as a case-insensitive set (blank names dropped, trimmed).</summary>
    internal static HashSet<string> ToValidCoreFireteamSet(IReadOnlyList<string> names) =>
        new(names.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);
}
