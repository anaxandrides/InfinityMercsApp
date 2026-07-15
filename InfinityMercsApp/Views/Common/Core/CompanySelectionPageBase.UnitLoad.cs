using System.Collections.ObjectModel;
using ArmyFactionRecord = InfinityMercsApp.Domain.Models.Army.Faction;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmySpecopsUnitRecord = InfinityMercsApp.Domain.Models.Army.SpecopsUnit;

namespace InfinityMercsApp.Views.Common;

public abstract partial class CompanySelectionPageBase
{
    /// <summary>
    /// Holds the combined results of a unit load operation: a dictionary of units keyed by
    /// their lookup key, and a dictionary of fireteam aggregates keyed by team name.
    /// </summary>
    protected sealed class CompanyMergedUnitsAndTeams
    {
        /// <summary>Units indexed by their unique lookup key for fast roster operations.</summary>
        public required Dictionary<string, ArmyUnitSelectionItem> UnitsByKey { get; init; }

        /// <summary>Fireteam aggregates indexed by team name, built from faction chart JSON.</summary>
        public required Dictionary<string, CompanyTeamAggregate> TeamsByName { get; init; }
    }

    /// <summary>
    /// Loads and merges resume and spec-ops units from all provided factions into a single
    /// <see cref="CompanyMergedUnitsAndTeams{TUnit}"/> result.
    /// Also caches unit logos and merges fireteam chart data as side effects.
    /// </summary>
    protected static async Task<CompanyMergedUnitsAndTeams> BuildMergedUnitsAndTeamsAsync<TFaction>(
        IReadOnlyCollection<TFaction> factions,
        Func<TFaction, int> readFactionId,
        Func<int, CancellationToken, IReadOnlyList<ArmyResumeRecord>> getResumeByFaction,
        Func<int, CancellationToken, Task<IReadOnlyList<ArmySpecopsUnitRecord>>> getSpecopsByFactionAsync,
        Func<int, CancellationToken, ArmyFactionRecord?> getFactionSnapshot,
        Func<int, IReadOnlyList<ArmyResumeRecord>, CancellationToken, Task> cacheUnitLogosAsync,
        Action<string?, Dictionary<string, CompanyTeamAggregate>> mergeFireteamEntries,
        Func<ArmyResumeRecord, IReadOnlyDictionary<int, string>, bool> isCharacterCategory,
        Func<ArmyResumeRecord, IReadOnlyDictionary<int, string>, IReadOnlyDictionary<int, string>, string?> buildUnitSubtitle,
        Func<int, ArmyResumeRecord, IReadOnlyDictionary<int, string>, IReadOnlyDictionary<int, string>, ArmyUnitSelectionItem> createResumeUnit,
        Func<int, ArmySpecopsUnitRecord, IReadOnlyDictionary<int, ArmyResumeRecord>, IReadOnlyList<ArmyResumeRecord>, IReadOnlyDictionary<int, string>, IReadOnlyDictionary<int, string>, ArmyUnitSelectionItem> createSpecopsUnit,
        CancellationToken cancellationToken)
    {
        // The full-roster assembly — per-faction DB reads, spec-ops merge, logo caching/rasterization,
        // and per-unit item construction — is heavy enough to freeze a faction tap for several seconds.
        // Run it off the UI thread (the DB layer serializes access with a lock, and only plain data
        // objects cross back), so the tap stays responsive.
        var merged = await Task.Run(
            () => CompanySelectionUnitLoadWorkflow.BuildMergedUnitsAndTeamsAsync(
                factions,
                readFactionId,
                getResumeByFaction,
                getSpecopsByFactionAsync,
                getFactionSnapshot,
                cacheUnitLogosAsync,
                mergeFireteamEntries,
                createResumeUnit,
                createSpecopsUnit,
                cancellationToken),
            cancellationToken).ConfigureAwait(false);

        return new CompanyMergedUnitsAndTeams
        {
            UnitsByKey = merged.UnitsByKey,
            TeamsByName = merged.TeamsByName
        };
    }

    /// <summary>
    /// Clears and repopulates the observable units collection from the merged unit set.
    /// Triggers binding updates for the unit list view.
    /// </summary>
    protected static void PopulateUnitsCollection(
        ICollection<ArmyUnitSelectionItem> unitsTarget,
        IEnumerable<ArmyUnitSelectionItem> mergedUnits)
    {
        CompanySelectionUnitLoadWorkflow.PopulateUnitsCollection(unitsTarget, mergedUnits);
    }

    /// <summary>
    /// Builds a dictionary of wildcard (non-team-specific) unit limits derived from the merged
    /// team aggregates, keyed by unit key. Used to populate the wildcard catch-all team entry.
    /// </summary>
    protected static Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> BuildWildcardUnitLimits(
        IEnumerable<CompanyTeamAggregate> mergedTeams,
        TeamUnitResolver<ArmyUnitSelectionItem> resolver)
    {
        return CompanySelectionUnitLoadWorkflow.BuildWildcardUnitLimits(mergedTeams, resolver);
    }

    /// <summary>
    /// Appends the wildcard team entry to the team list, representing units that can fill
    /// any fireteam slot. Only adds the entry when at least one wildcard limit exists.
    /// </summary>
    protected static void AppendWildcardTeamEntry(
        Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> wildcardUnitLimits,
        TeamUnitResolver<ArmyUnitSelectionItem> resolver,
        ICollection<ArmyTeamListItem> teamEntries,
        Func<string, string, string, string?, TeamUnitResolver<ArmyUnitSelectionItem>, ArmyTeamUnitLimitItem> buildTeamUnitLimitItem,
        Func<string, string, bool, bool, ObservableCollection<ArmyTeamUnitLimitItem>, ArmyTeamListItem> createTeam)
    {
        CompanySelectionUnitLoadWorkflow.AppendWildcardTeamEntry(
            wildcardUnitLimits,
            resolver,
            teamEntries,
            buildTeamUnitLimitItem,
            createTeam);
    }

    /// <summary>
    /// Converts the merged team aggregates into concrete team list items and adds them to
    /// <paramref name="teamEntries"/>. Teams are filtered and sorted according to the
    /// <paramref name="includeTeam"/> predicate and count accessors.
    /// </summary>
    protected static async Task BuildTeamEntriesFromMergedAsync(
        CompanyMergedUnitsAndTeams merged,
        ICollection<ArmyTeamListItem> teamEntries,
        Func<CompanyTeamAggregate, bool> includeTeam,
        Func<CompanyTeamAggregate, int> readTeamCount,
        Func<CompanyTeamAggregate, string> buildTeamCountText,
        Func<string, string, string, string?, TeamUnitResolver<ArmyUnitSelectionItem>, ArmyTeamUnitLimitItem> buildTeamUnitLimitItem,
        Func<string, string, bool, bool, ObservableCollection<ArmyTeamUnitLimitItem>, ArmyTeamListItem> createTeam,
        CancellationToken cancellationToken = default)
    {
        // Matching every unit against every team's unit limits is an O(teams × limits × units)
        // computation that was the single biggest faction-tap stall. Build the team-entry data
        // objects on a worker thread, then add them to the UI-bound collection on the UI thread.
        var built = await Task.Run(
            () => CompanySelectionUnitLoadWorkflow.BuildTeamEntriesFromMerged(
                merged.UnitsByKey,
                merged.TeamsByName.Values,
                includeTeam,
                readTeamCount,
                buildTeamCountText,
                buildTeamUnitLimitItem,
                createTeam),
            cancellationToken);

        foreach (var item in built)
        {
            teamEntries.Add(item);
        }
    }
}
