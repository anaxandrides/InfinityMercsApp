using InfinityMercsApp.Services.CompanySelection.Recruitment;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection;
using Microsoft.AspNetCore.Components;

namespace InfinityMercsApp.Web.Platform;

/// <inheritdoc/>
/// <remarks>Keeps the new selection screen in the session and opens the selection page, which shows it.</remarks>
public sealed class WebCompanySelectionNavigator(CompanySelectionScreens screens, CompanySelectionSession session, NavigationManager navigation)
    : ICompanySelectionNavigator
{
    public Task OpenAsync(CompanyKind kind, ArmySourceSelectionMode mode = ArmySourceSelectionMode.Sectorials)
    {
        session.Current = screens.For(kind, mode);
        navigation.NavigateTo(WebRoutes.CompanySelection);
        return Task.CompletedTask;
    }

    public Task<bool> OpenRecruitmentAsync(CompanyRecruitment recruitment)
    {
        if (screens.ForRecruitment(recruitment) is not { } viewModel)
        {
            return Task.FromResult(false);
        }

        session.Current = viewModel;
        navigation.NavigateTo(WebRoutes.CompanySelection);
        return Task.FromResult(true);
    }
}
