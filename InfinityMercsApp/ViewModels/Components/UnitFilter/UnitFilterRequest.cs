namespace InfinityMercsApp.ViewModels.Components.UnitFilter;

/// <summary>What the filter popup opens with: the values to choose from and the filter already applied.</summary>
public sealed record UnitFilterRequest(
    UnitFilterPopupOptions Options,
    UnitFilterCriteria ExistingCriteria,
    bool LieutenantOnlyUnits,
    bool TeamsView = false,
    bool TeamsViewEnabled = true,
    bool LieutenantOnlyUnitsEnabled = true);
