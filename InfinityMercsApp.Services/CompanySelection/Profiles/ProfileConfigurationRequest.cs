using InfinityMercsApp.Domain.Models.Army.Filters;

namespace InfinityMercsApp.Services.CompanySelection.Profiles;

/// <summary>A unit's profiles, the filters naming their entries, and the company's rules for listing them.</summary>
public sealed class ProfileConfigurationRequest
{
    public required UnitProfileGroups ProfileGroups { get; init; }
    public required ArmyFilterCatalog Filters { get; init; }

    /// <summary>Every configuration counts as a lieutenant (a Spec-Ops unit).</summary>
    public required bool ForceLieutenant { get; init; }

    public required bool ShowUnitsInInches { get; init; }

    /// <summary>Only fireteam-only (FTO) options are listed, as when the unit was picked from an FTO fireteam slot.</summary>
    public bool FireteamOnly { get; init; }

    /// <summary>
    /// Points off an option with Tactical Awareness (on the option, what it includes, or its group's profiles),
    /// after the peripheral adjustment; a Cohesive company's rule.
    /// </summary>
    public int TacticalAwarenessDiscount { get; init; }
}
