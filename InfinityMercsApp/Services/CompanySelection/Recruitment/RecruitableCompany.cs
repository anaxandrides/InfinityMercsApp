using InfinityMercsApp.Services.Navigation;

namespace InfinityMercsApp.Services.CompanySelection.Recruitment;

/// <summary>The selection screen a saved company recruits through: its kind, and where its units come from.</summary>
public sealed record RecruitableCompany(CompanyKind Kind, ArmySourceSelectionMode Mode)
{
    /// <summary>The screen for a saved company type, as the selection screens write it; null for a type they don't build.</summary>
    public static RecruitableCompany? For(string? companyType) => companyType?.Trim() switch
    {
        "Standard Company - Vanilla" => new(CompanyKind.Standard, ArmySourceSelectionMode.VanillaFactions),
        "Standard Company - Sectorial" => new(CompanyKind.Standard, ArmySourceSelectionMode.Sectorials),
        "TAG Company" => new(CompanyKind.Tag, ArmySourceSelectionMode.Sectorials),
        "Lone Wolf Company" => new(CompanyKind.LoneWolf, ArmySourceSelectionMode.VanillaFactions),
        "Cohesive Company" => new(CompanyKind.Cohesive, ArmySourceSelectionMode.Sectorials),
        "Inspiring Leader" => new(CompanyKind.Inspiring, ArmySourceSelectionMode.Sectorials),
        "Airborne Company" => new(CompanyKind.Airborne, ArmySourceSelectionMode.Sectorials),
        _ => null
    };
}
