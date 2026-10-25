namespace InfinityMercsApp.ViewModels.Company.Selection;

/// <summary>What loading the selected unit's details depends on besides the unit.</summary>
/// <param name="LieutenantOnlyUnits">Whether the filter shows lieutenants only.</param>
/// <param name="AvaFactionIds">The factions whose AVA the company goes by for the unit.</param>
/// <param name="TacticalAwarenessDiscount">Points off a profile with Tactical Awareness.</param>
public sealed record UnitPreviewRequest(bool LieutenantOnlyUnits, IReadOnlyList<int> AvaFactionIds, int TacticalAwarenessDiscount);
