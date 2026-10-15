using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.Views;

namespace InfinityMercsApp.Services.Navigation;

/// <summary>Opens the company selection screen with the ViewModel for the kind of company being built.</summary>
internal sealed class CompanySelectionNavigator(CompanySelectionServices services) : ICompanySelectionNavigator
{
    public Task OpenAsync(CompanyKind kind, ArmySourceSelectionMode mode = ArmySourceSelectionMode.Sectorials)
    {
        var navigation = Shell.Current?.Navigation
            ?? throw new InvalidOperationException("No active Shell is available to open the company selection.");
        return navigation.PushAsync(new CompanySelectionPage(ViewModelFor(kind, mode)));
    }

    /// <summary>The ViewModel for a kind of company; Inspiring and Airborne companies always use sectorials.</summary>
    internal CompanySelectionViewModel ViewModelFor(CompanyKind kind, ArmySourceSelectionMode mode) => kind switch
    {
        CompanyKind.Standard => new StandardCompanySelectionViewModel(services, mode),
        CompanyKind.Tag => new TagCompanySelectionViewModel(services, mode),
        CompanyKind.LoneWolf => new LoneWolfCompanySelectionViewModel(services, mode),
        CompanyKind.Cohesive => new CohesiveCompanySelectionViewModel(services, mode),
        CompanyKind.Inspiring => new InspiringCompanySelectionViewModel(services),
        CompanyKind.Airborne => new AirborneCompanySelectionViewModel(services),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}
