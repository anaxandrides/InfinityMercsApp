using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Presentation.Season;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels.Season;

/// <summary>The MERCS Season home: load a season, or start one from a saved or new company.</summary>
public partial class MercsSeasonPageViewModel(INavigationService navigationService) : AdaptiveViewModelBase<MercsSeasonLayout>(navigationService, MercsSeasonLayout.For)
{
    [ObservableProperty]
    public partial bool IsChoosingNewSeason { get; set; }

    /// <summary>Always return to the main two-button state when the page reappears.</summary>
    public override Task InitializeAsync()
    {
        ShowMainActions();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task LoadSeasonAsync() => NavigationService.PushAsync("LoadSeasonPage");

    [RelayCommand]
    private void StartNewSeason() => IsChoosingNewSeason = true;

    [RelayCommand]
    private void CancelNewSeason() => ShowMainActions();

    [RelayCommand]
    private Task LoadCompanyAsync() => NavigationService.NavigateToAsync(
        "//LoadCompanyPage",
        new Dictionary<string, object> { [NavigationParameters.SeasonMode] = true });

    [RelayCommand]
    private Task CreateNewTeamAsync() => NavigationService.NavigateToAsync("//CreateNewCompanyPage");

    private void ShowMainActions() => IsChoosingNewSeason = false;
}
