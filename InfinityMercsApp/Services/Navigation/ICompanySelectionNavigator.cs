using InfinityMercsApp.Views;

namespace InfinityMercsApp.Services.Navigation;

/// <summary>Opens the unit-selection screen for a new company.</summary>
public interface ICompanySelectionNavigator
{
    /// <param name="mode">Where units come from; ignored by Inspiring and Airborne companies.</param>
    Task OpenAsync(CompanyKind kind, ArmySourceSelectionMode mode = ArmySourceSelectionMode.Sectorials);
}
