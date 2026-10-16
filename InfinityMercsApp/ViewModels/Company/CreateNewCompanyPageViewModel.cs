using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels.Company;

/// <summary>Picks the kind of company to build and opens its selection screen.</summary>
public partial class CreateNewCompanyPageViewModel : AdaptiveViewModelBase<CreateNewCompanyLayout>
{
    private readonly ICompanySelectionNavigator _companySelection;

    public CreateNewCompanyPageViewModel(INavigationService navigationService, ICompanySelectionNavigator companySelection)
        : base(navigationService, CreateNewCompanyLayout.For)
    {
        _companySelection = companySelection;
        LayOutChoices();
    }

    /// <summary>The cards in display order. Lone Wolf has a command but no card until it is ready.</summary>
    [ObservableProperty]
    private IReadOnlyList<CompanyChoice> _choices = [];

    [ObservableProperty]
    private string _choiceRows = string.Empty;

    protected override void OnLayoutApplied() => LayOutChoices();

    private void LayOutChoices()
    {
        Choices = ChoicesFor(Layout);
        ChoiceRows = Layout.GridRowsFor(Choices.Count);
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
    private Task OpenCohesiveCompanyPageAsync() => _companySelection.OpenAsync(CompanyKind.Cohesive, ArmySourceSelectionMode.Sectorials);

    [RelayCommand]
    private Task OpenInspiringLeadershipPageAsync() => _companySelection.OpenAsync(CompanyKind.Inspiring);

    [RelayCommand]
    private Task OpenAirborneCompanyPageAsync() => _companySelection.OpenAsync(CompanyKind.Airborne);

    [RelayCommand]
    private Task OpenTAGCompanyPageAsync() => _companySelection.OpenAsync(CompanyKind.Tag, ArmySourceSelectionMode.Sectorials);

    [RelayCommand]
    private Task OpenLoneWolfCompanyPopupAsync() => _companySelection.OpenAsync(CompanyKind.LoneWolf, ArmySourceSelectionMode.VanillaFactions);
}
