using InfinityMercsApp.Domain.Models.Army.Filters;

namespace InfinityMercsApp.Services.CompanySelection.Units;

internal sealed class CompanyUnitDetailsWorkflowRequest
{
    public required string UnitName { get; init; }
    public required int UnitId { get; init; }
    public required int SourceFactionId { get; init; }
    public required bool IsSpecOps { get; init; }
    public required bool LieutenantOnlyUnits { get; init; }
    public required bool ShowUnitsInInches { get; init; }
    public required Func<int, int, CancellationToken, InfinityMercsApp.Domain.Models.Army.Unit?> GetUnit { get; init; }
    public required Func<int, CancellationToken, InfinityMercsApp.Domain.Models.Army.Faction?> GetFactionSnapshot { get; init; }
    public required Func<int, CancellationToken, IReadOnlyList<InfinityMercsApp.Domain.Models.Army.SpecOps.SpecopsUnit>> GetSpecopsUnitsByFaction { get; init; }
    public required Action<int, InfinityMercsApp.Domain.Models.Army.Unit?, CancellationToken> ApplyUnitHeaderColors { get; init; }
    public required Action<CancellationToken> ApplyGlobalDisplayUnitsPreference { get; init; }
    public required Action<UnitProfileGroups> PopulateUnitStatsFromFirstProfile { get; init; }
    public required Action<bool, bool, bool, bool> SetOrderTraits { get; init; }
    public required Action<bool, bool, bool> SetTechTraits { get; init; }
    public required Func<IEnumerable<string>, List<string>> EnsureLieutenantSkill { get; init; }
    public required Action<IReadOnlyList<string>, IReadOnlyList<string>, bool> SetCommonEquipmentSkills { get; init; }
    public required Action<string, string> SetSummaryText { get; init; }
    public required Action RefreshSummaryFormatted { get; init; }
    public required Action<UnitProfileGroups, ArmyFilterCatalog, bool> PopulateProfilesFromProfileGroups { get; init; }
    public required Action UpdatePeripheralStatBlockFromVisibleProfiles { get; init; }
    public required Action<UnitProfileGroups> SetSelectedUnitProfileGroups { get; init; }
    public required Action<ArmyFilterCatalog> SetSelectedUnitFilters { get; init; }
    public required Action<string> SetUnitNameHeading { get; init; }
    public required Action<bool> SetSummaryHighlightLieutenant { get; init; }
    public required Action<string> LogInfo { get; init; }
    public required Action<string> LogError { get; init; }
}
