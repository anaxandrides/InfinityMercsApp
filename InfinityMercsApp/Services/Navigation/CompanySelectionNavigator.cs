using InfinityMercsApp.Services.CompanySelection.Recruitment;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.Views.Company.Selection;

namespace InfinityMercsApp.Services.Navigation;

/// <summary>Opens the company selection screen with the ViewModel for the kind of company being built.</summary>
internal sealed class CompanySelectionNavigator(CompanySelectionScreens screens) : ICompanySelectionNavigator
{
    public Task OpenAsync(CompanyKind kind, ArmySourceSelectionMode mode = ArmySourceSelectionMode.Sectorials)
    {
        var navigation = Shell.Current?.Navigation
            ?? throw new InvalidOperationException("No active Shell is available to open the company selection.");
        return navigation.PushAsync(new CompanySelectionPage(screens.For(kind, mode)));
    }

    public async Task<bool> OpenRecruitmentAsync(CompanyRecruitment recruitment)
    {
        if (screens.ForRecruitment(recruitment) is not { } viewModel)
        {
            return false;
        }

        var navigation = Shell.Current?.Navigation
            ?? throw new InvalidOperationException("No active Shell is available to open recruitment.");
        await navigation.PushAsync(new CompanySelectionPage(viewModel));
        return true;
    }
}
