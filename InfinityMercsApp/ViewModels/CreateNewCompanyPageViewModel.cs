using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.Views;

namespace InfinityMercsApp.ViewModels;

/// <summary>Picks the kind of company to build and opens its selection screen.</summary>
public partial class CreateNewCompanyPageViewModel(
    INavigationService navigationService,
    ICompanySelectionNavigator companySelection) : ViewModelBase(navigationService)
{
    [RelayCommand]
    private Task NavigateToModeSelectionPageAsync() => NavigationService.NavigateToAsync("//MercsSeasonPage");

    [RelayCommand]
    private Task OpenStandardCompanyPopupAsync() => NavigationService.NavigateToAsync("//StandardCompanySourcePopupPage");

    [RelayCommand]
    private Task OpenCohesiveCompanyPageAsync() => companySelection.OpenAsync(CompanyKind.Cohesive, ArmySourceSelectionMode.Sectorials);

    [RelayCommand]
    private Task OpenInspiringLeadershipPageAsync() => companySelection.OpenAsync(CompanyKind.Inspiring);

    [RelayCommand]
    private Task OpenAirborneCompanyPageAsync() => companySelection.OpenAsync(CompanyKind.Airborne);

    [RelayCommand]
    private Task OpenTAGCompanyPageAsync() => companySelection.OpenAsync(CompanyKind.Tag, ArmySourceSelectionMode.Sectorials);

    [RelayCommand]
    private Task OpenLoneWolfCompanyPopupAsync() => companySelection.OpenAsync(CompanyKind.LoneWolf, ArmySourceSelectionMode.VanillaFactions);
}
