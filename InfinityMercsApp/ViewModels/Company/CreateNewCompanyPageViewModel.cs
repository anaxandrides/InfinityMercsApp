using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.Views;

namespace InfinityMercsApp.ViewModels.Company;

/// <summary>Picks the kind of company to build and opens its selection screen.</summary>
public partial class CreateNewCompanyPageViewModel(
    INavigationService navigationService,
    ICompanySelectionNavigator companySelection)
    : AdaptiveViewModelBase<CreateNewCompanyLayout>(navigationService, CreateNewCompanyLayout.For)
{
    private IReadOnlyList<CompanyChoice>? _choices;

    /// <summary>The cards in display order. Lone Wolf has a command but no card until it is ready.</summary>
    public IReadOnlyList<CompanyChoice> Choices => _choices ??= ChoicesFor(Layout);

    public string ChoiceRows => Layout.GridRowsFor(Choices.Count);

    protected override void OnLayoutApplied()
    {
        _choices = null;
        OnPropertiesChanged(nameof(Choices), nameof(ChoiceRows));
    }

    private IReadOnlyList<CompanyChoice> ChoicesFor(CreateNewCompanyLayout layout)
    {
        List<(string Title, string Icon, IAsyncRelayCommand Command)> cards =
        [
            ("Standard Company", "MercsIcons/standard_company.svg", OpenStandardCompanyPopupCommand),
            ("Cohesive Company", "MercsIcons/cohesive_company.svg", OpenCohesiveCompanyPageCommand),
            ("Inspiring Leader", "MercsIcons/inspiring_leadership.svg", OpenInspiringLeadershipPageCommand),
            ("Airborne Company", "MercsIcons/airborne_company.svg", OpenAirborneCompanyPageCommand),
            ("TAG Company", "MercsIcons/tag_company.svg", OpenTAGCompanyPageCommand)
        ];
        return cards.Select((card, index) => new CompanyChoice(card.Title, card.Icon, card.Command, layout.CellFor(index), layout.Card)).ToList();
    }

    [RelayCommand]
    private Task NavigateBackAsync() => NavigationService.NavigateToAsync("//MercsSeasonPage");

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
