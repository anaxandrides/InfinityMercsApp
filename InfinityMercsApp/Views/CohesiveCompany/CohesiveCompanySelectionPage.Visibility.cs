using InfinityMercsApp.Services.CompanySelection.Units;

namespace InfinityMercsApp.Views.CohesiveCompany;

public partial class CohesiveCompanySelectionPage
{
    private void ApplyUnitVisibilityFilters(CancellationToken cancellationToken = default)
    {
        AreTeamEntriesReady = false;
        if (Units.Count == 0)
        {
            return;
        }

        try
        {
            var pointsRemaining = ComputePointsRemaining(this);

            var factions = CompanyUnitDetailsShared.BuildUnitSourceFactions(
                ShowRightSelectionBox,
                _factionSelectionState.LeftSlotFaction,
                _factionSelectionState.RightSlotFaction,
                faction => faction.Id);
            var lookupContext = BuildUnitVisibilityLookupContext(
                factions,
                faction => faction.Id,
                GetFactionSnapshotFromProvider,
                _specOpsProvider.GetSpecopsUnitsByFaction,
                cancellationToken);

            ApplyUnitVisibilityCore(
                Units,
                lookupContext,
                _filterState.ActiveUnitFilter,
                LieutenantOnlyUnits,
                pointsRemaining,
                GetUnitFromProvider,
                cancellationToken);

            var visibleCount = Units.Count(x => x.IsVisible);
            Console.WriteLine(
                $"[CohesiveCompanySelectionPage] Visibility result: {visibleCount}/{Units.Count} visible " +
                $"(pointsRemaining={pointsRemaining}, filterActive={_filterState.ActiveUnitFilter.IsActive}, lieutenantOnly={LieutenantOnlyUnits}).");

            if (visibleCount == 0 && Units.Count > 0)
            {
                Console.WriteLine("[CohesiveCompanySelectionPage] No visible units after filtering. Forcing loaded units visible.");
                foreach (var unit in Units)
                {
                    unit.IsVisible = true;
                }
            }

            _selectedUnit = RefreshSelectedUnitVisibilityCore(
                _selectedUnit,
                () => ResetUnitDetails(),
                ApplyLieutenantVisualStates);

            RefreshTeamEntryVisibilityCore(
                TeamEntries,
                Units);
            AreTeamEntriesReady = true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanySelectionPage ApplyUnitVisibilityFilters failed: {ex.Message}");
            AreTeamEntriesReady = false;
        }
    }
}
