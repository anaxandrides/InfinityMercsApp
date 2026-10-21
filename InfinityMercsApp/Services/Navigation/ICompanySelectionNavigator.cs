using InfinityMercsApp.Services.CompanySelection.Recruitment;

namespace InfinityMercsApp.Services.Navigation;

/// <summary>Opens the unit-selection screen for a new company, or for an existing one hiring recruits.</summary>
public interface ICompanySelectionNavigator
{
    /// <param name="mode">Where units come from; ignored by Inspiring and Airborne companies.</param>
    Task OpenAsync(CompanyKind kind, ArmySourceSelectionMode mode = ArmySourceSelectionMode.Sectorials);

    /// <summary>Opens the company's own kind of selection screen to hire new troopers.</summary>
    /// <returns>False when the company's type has no selection screen, so nothing opened.</returns>
    Task<bool> OpenRecruitmentAsync(CompanyRecruitment recruitment);
}
