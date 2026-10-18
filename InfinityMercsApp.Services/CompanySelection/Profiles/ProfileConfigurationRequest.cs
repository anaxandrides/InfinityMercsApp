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

    /// <summary>The company's option filter (e.g. fireteam-only profiles), given the option's name.</summary>
    public required Func<string, bool> IncludesOption { get; init; }

    /// <summary>The company's adjustment to an option's cost, applied after the peripheral adjustment.</summary>
    public Func<ProfileGroup, ProfileOption, string, string>? AdjustCost { get; init; }
}
