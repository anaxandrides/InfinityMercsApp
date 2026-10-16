using InfinityMercsApp.Domain.Models.Army.Filters;

namespace InfinityMercsApp.ViewModels.Company.Selection.Workflows;

/// <summary>
/// What a page supplies to build its profile list: the unit, the army filters, and the page-specific rules.
/// </summary>
internal sealed class CompanyProfilePopulationRequest
{
    public required UnitProfileGroups ProfileGroups { get; init; }
    public required ArmyFilterCatalog Filters { get; init; }
    public required bool ForceLieutenant { get; init; }
    public required bool ShowTacticalAwarenessIcon { get; init; }
    public required bool ShowUnitsInInches { get; init; }

    /// <summary>Page-specific option filter (e.g. FTO-only); receives the option's display name.</summary>
    public required Func<ProfileGroup, ProfileOption, string, bool> ShouldIncludeOption { get; init; }

    /// <summary>Optional page-specific cost adjustment applied after the peripheral adjustment.</summary>
    public Func<ProfileGroup, ProfileOption, string, string>? AdjustOptionDisplayCost { get; init; }

    public required Func<int?, int?, string> FormatMoveValue { get; init; }
}
