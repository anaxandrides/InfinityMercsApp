using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Base;

using InfinityMercsApp.Presentation.Company;
namespace InfinityMercsApp.ViewModels.Company;

/// <summary>Chooses where a Standard company's units come from: one vanilla faction or two sectorials.</summary>
public partial class StandardCompanySourcePopupPageViewModel(
    INavigationService navigationService,
    ICompanySelectionNavigator companySelection) : AdaptiveViewModelBase<SourcePopupLayout>(navigationService, SourcePopupLayout.For)
{
    [RelayCommand]
    private Task ClosePopupAsync() => NavigationService.NavigateToAsync("//CreateNewCompanyPage");

    [RelayCommand]
    private Task VanillaFactionSelectedAsync() => OpenSelectionAsync(ArmySourceSelectionMode.VanillaFactions);

    [RelayCommand]
    private Task TwoSectorialsSelectedAsync() => OpenSelectionAsync(ArmySourceSelectionMode.Sectorials);

    private async Task OpenSelectionAsync(ArmySourceSelectionMode mode)
    {
        try
        {
            await companySelection.OpenAsync(CompanyKind.Standard, mode);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[StandardCompanySourcePopupPage] Failed to navigate to faction selection page: {ex.Message}");
        }
    }
}
