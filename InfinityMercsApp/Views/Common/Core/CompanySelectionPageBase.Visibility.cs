using InfinityMercsApp.Views.Controls;
using ArmyFactionRecord = InfinityMercsApp.Domain.Models.Army.Faction;
using ArmySpecopsUnitRecord = InfinityMercsApp.Domain.Models.Army.SpecopsUnit;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;

namespace InfinityMercsApp.Views.Common;

public abstract partial class CompanySelectionPageBase
{
    /// <summary>
    /// Computes the points remaining by parsing and subtracting the current points total
    /// from the season cap. Returns 0 when either value cannot be parsed.
    /// </summary>
    protected static int ComputePointsRemaining(string selectedStartSeasonPoints, string seasonPointsCapText)
    {
        return CompanySelectionVisibilityWorkflow.ComputePointsRemaining(selectedStartSeasonPoints, seasonPointsCapText);
    }

    /// <summary>
    /// Overload that reads the season points values from an <see cref="ICompanySelectionVisibilityState"/>.
    /// </summary>
    protected static int ComputePointsRemaining(ICompanySelectionVisibilityState state)
    {
        return CompanySelectionVisibilityWorkflow.ComputePointsRemaining(state);
    }

    /// <summary>
    /// Asynchronously builds the lookup context used by <see cref="ApplyUnitVisibilityCore{TUnit}"/>,
    /// pre-fetching faction snapshots and spec-ops unit lists for all active factions.
    /// The context is reused across multiple visibility passes to avoid redundant data access.
    /// </summary>
    protected static async Task<CompanyUnitVisibilityLookupContext> BuildUnitVisibilityLookupContextAsync<TFaction>(
        IEnumerable<TFaction> factions,
        Func<TFaction, int> readFactionId,
        Func<int, CancellationToken, ArmyFactionRecord?> getFactionSnapshot,
        Func<int, CancellationToken, Task<IReadOnlyList<ArmySpecopsUnitRecord>>> getSpecopsUnitsByFactionAsync,
        CancellationToken cancellationToken)
    {
        // Per-faction snapshot fetch + FiltersJson parsing (skills/type/chars/equip/weapons/ammo
        // lookups) is heavy enough to stall the faction tap; run it off the UI thread.
        return await Task.Run(
            () => CompanySelectionVisibilityWorkflow.BuildUnitVisibilityLookupContextAsync(
                factions,
                readFactionId,
                getFactionSnapshot,
                getSpecopsUnitsByFactionAsync,
                cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Evaluates each unit in the list and updates its <c>IsVisible</c> flag based on
    /// the active filter criteria, lieutenant restriction, and remaining points budget.
    /// </summary>
    protected static void ApplyUnitVisibilityCore(
        IEnumerable<ArmyUnitSelectionItem> units,
        CompanyUnitVisibilityLookupContext lookupContext,
        UnitFilterCriteria activeUnitFilter,
        bool lieutenantOnlyUnits,
        int pointsRemaining,
        Func<int, int, CancellationToken, ArmyUnitRecord?> getUnitByFactionAndId,
        CancellationToken cancellationToken)
    {
        CompanySelectionVisibilityWorkflow.ApplyUnitVisibility(
            units,
            lookupContext,
            activeUnitFilter,
            lieutenantOnlyUnits,
            pointsRemaining,
            getUnitByFactionAndId,
            cancellationToken);
    }

    /// <summary>
    /// Off-UI-thread variant of <see cref="ApplyUnitVisibilityCore"/>: the dominant faction-tap
    /// cost is the per-unit DB fetch + profile JSON parse, so that computation runs on a worker
    /// thread and only the resulting <c>IsVisible</c> writes (a bound property) happen on the UI
    /// thread. Callers must <c>await</c> this from the UI thread so the apply resumes there.
    /// </summary>
    protected static async Task ApplyUnitVisibilityCoreAsync(
        IEnumerable<ArmyUnitSelectionItem> units,
        CompanyUnitVisibilityLookupContext lookupContext,
        UnitFilterCriteria activeUnitFilter,
        bool lieutenantOnlyUnits,
        int pointsRemaining,
        Func<int, int, CancellationToken, ArmyUnitRecord?> getUnitByFactionAndId,
        CancellationToken cancellationToken)
    {
        var snapshot = units.ToList();

        var decisions = await Task.Run(
            () => CompanySelectionVisibilityWorkflow.ComputeUnitVisibility(
                snapshot,
                lookupContext,
                activeUnitFilter,
                lieutenantOnlyUnits,
                pointsRemaining,
                getUnitByFactionAndId,
                cancellationToken),
            cancellationToken);

        foreach (var (unit, isVisible) in decisions)
        {
            unit.IsVisible = isVisible;
        }
    }

    /// <summary>
    /// Checks whether the currently selected unit is still visible after a filter change.
    /// If hidden, resets the unit details panel and re-applies lieutenant visual states.
    /// Returns the selected unit unchanged if it is still visible, or <c>null</c> if it was hidden.
    /// </summary>
    protected static ArmyUnitSelectionItem? RefreshSelectedUnitVisibilityCore(
        ArmyUnitSelectionItem? selectedUnit,
        Action resetUnitDetails,
        Action applyLieutenantVisualStates)
    {
        return CompanySelectionVisibilityWorkflow.RefreshSelectedUnitVisibility(
            selectedUnit,
            resetUnitDetails,
            applyLieutenantVisualStates);
    }

    /// <summary>
    /// Updates the visible unit count on each team entry by cross-referencing the current
    /// unit visibility states, allowing the team list to reflect the active filter.
    /// </summary>
    protected static void RefreshTeamEntryVisibilityCore(
        IEnumerable<ArmyTeamListItem> teamEntries,
        IEnumerable<ArmyUnitSelectionItem> units)
    {
        CompanySelectionVisibilityWorkflow.RefreshTeamEntryVisibility(
            teamEntries,
            units);
    }
}
