using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Units;

namespace InfinityMercsApp.Services.CompanySelection.Teams;

/// <summary>Which of a faction's Core fireteams a Cohesive company can field under its filter and points.</summary>
public sealed class CoreFireteamEligibility(IArmyDataService armyData)
{
    private sealed class EligibilitySourceUnit
    {
        public required int Id { get; init; }
        public required string? Name { get; init; }
        public required string? Slug { get; init; }
        public required int? Type { get; init; }
        public required bool IsCharacter { get; init; }
    }

    /// <summary>
    /// The faction's Core fireteams (by name) with a non-character unit that has a lieutenant profile without SWC
    /// fitting <paramref name="filter"/> and <paramref name="maxCost"/>; a fireteam-only slot needs a fireteam-only profile.
    /// </summary>
    public List<string> ValidCoreFireteams(int factionId, UnitFilterCriteria filter, int maxCost, CancellationToken cancellationToken = default)
    {
        var snapshot = armyData.GetFactionSnapshot(factionId, cancellationToken);
        if (snapshot?.FireteamChart is null)
        {
            return [];
        }

        var lookups = UnitFilterLookups.From(snapshot.Filters);
        var typeLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot.Filters, ArmyFilterSection.Type);
        var categoryLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot.Filters, ArmyFilterSection.Category);
        var sourceUnits = armyData.GetResumeByFactionMercsOnly(factionId, cancellationToken).Select(unit => new EligibilitySourceUnit
            {
                Id = unit.UnitId,
                Name = unit.Name,
                Slug = unit.Slug,
                Type = unit.Type,
                IsCharacter = CompanyUnitDetailsShared.IsCharacterCategory(unit, categoryLookup)
            })
            .ToList();

        var teams = new Dictionary<string, MergedFireteam>(StringComparer.OrdinalIgnoreCase);
        MergedFireteam.MergeChart(snapshot.FireteamChart, teams);

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

                if (!CompanyUnitDetailsShared.MatchesClassificationFilter(filter, matchedUnit.Type, typeLookup))
                {
                    continue;
                }

                var unitRecord = armyData.GetUnit(factionId, matchedUnit.Id, cancellationToken);
                if (unitRecord?.ProfileGroups is not { IsEmpty: false })
                {
                    continue;
                }

                var requiresFtoProfile = FireteamMatching.IsFtoLabel(unitLimit.Key);
                var hasAnyVisibleProfile = UnitOptionFilter.HasVisibleOption(
                    unitRecord.ProfileGroups,
                    lookups,
                    filter,
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
                    filter,
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
    public static HashSet<string> ToValidCoreFireteamSet(IReadOnlyList<string> names) =>
        new(names.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);
}
