using InfinityMercsApp.Views;

namespace InfinityMercsApp.Services;

/// <summary>The kinds of company that have their own unit-selection screen.</summary>
public enum CompanyKind
{
    Standard,
    Tag,
    LoneWolf,
    Cohesive,
    Inspiring,
    Airborne
}

/// <summary>Opens the unit-selection screen for a new company.</summary>
public interface ICompanySelectionNavigator
{
    /// <param name="mode">Where units come from; ignored by Inspiring and Airborne companies.</param>
    Task OpenAsync(CompanyKind kind, ArmySourceSelectionMode mode = ArmySourceSelectionMode.Sectorials);
}
