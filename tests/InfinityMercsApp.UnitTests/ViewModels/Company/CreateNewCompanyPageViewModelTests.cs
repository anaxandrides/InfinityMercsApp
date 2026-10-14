using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.ViewModels.Company;
using InfinityMercsApp.Views;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Company;

public sealed class CreateNewCompanyPageViewModelTests
{
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly ICompanySelectionNavigator _companySelection = Substitute.For<ICompanySelectionNavigator>();

    private CreateNewCompanyPageViewModel Create() => new(_navigation, _companySelection);

    [Fact]
    public async Task StandardOpensTheSourcePopup()
    {
        await Create().OpenStandardCompanyPopupCommand.ExecuteAsync(null);

        await _navigation.Received(1).NavigateToAsync("//StandardCompanySourcePopupPage");
        await _companySelection.DidNotReceiveWithAnyArgs().OpenAsync(default);
    }

    [Fact]
    public async Task BackReturnsToTheSeasonHome()
    {
        await Create().NavigateBackCommand.ExecuteAsync(null);

        await _navigation.Received(1).NavigateToAsync("//MercsSeasonPage");
    }

    [Theory]
    [InlineData(nameof(CreateNewCompanyPageViewModel.OpenCohesiveCompanyPageCommand), CompanyKind.Cohesive, ArmySourceSelectionMode.Sectorials)]
    [InlineData(nameof(CreateNewCompanyPageViewModel.OpenInspiringLeadershipPageCommand), CompanyKind.Inspiring, ArmySourceSelectionMode.Sectorials)]
    [InlineData(nameof(CreateNewCompanyPageViewModel.OpenAirborneCompanyPageCommand), CompanyKind.Airborne, ArmySourceSelectionMode.Sectorials)]
    [InlineData(nameof(CreateNewCompanyPageViewModel.OpenTAGCompanyPageCommand), CompanyKind.Tag, ArmySourceSelectionMode.Sectorials)]
    [InlineData(nameof(CreateNewCompanyPageViewModel.OpenLoneWolfCompanyPopupCommand), CompanyKind.LoneWolf, ArmySourceSelectionMode.VanillaFactions)]
    public async Task OtherCompaniesOpenTheirSelectionScreens(string command, CompanyKind kind, ArmySourceSelectionMode mode)
    {
        var viewModel = Create();
        var relayCommand = (CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)typeof(CreateNewCompanyPageViewModel).GetProperty(command)!.GetValue(viewModel)!;

        await relayCommand.ExecuteAsync(null);

        await _companySelection.Received(1).OpenAsync(kind, mode);
        await _navigation.DidNotReceiveWithAnyArgs().NavigateToAsync(default!, default);
    }

    [Fact]
    public void ListsTheFiveCompanyCardsInOneColumnOnAPhone()
    {
        var viewModel = Create();

        Assert.Equal(["Standard Company", "Cohesive Company", "Inspiring Leader", "Airborne Company", "TAG Company"], viewModel.Choices.Select(choice => choice.Title));
        Assert.Same(viewModel.OpenTAGCompanyPageCommand, viewModel.Choices[4].Command);
        Assert.Equal(new GridCell(4, 0), viewModel.Choices[4].Cell);
        Assert.Equal("Auto,Auto,Auto,Auto,Auto", viewModel.ChoiceRows);
    }

    [Fact]
    public void ReflowsTheCardsForTheWidth()
    {
        var viewModel = Create();
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.ApplyLayoutCommand.Execute(AdaptiveLayoutMode.Wide);

        Assert.Equal(new GridCell(1, 1), viewModel.Choices[4].Cell);
        Assert.Equal("Auto,Auto", viewModel.ChoiceRows);
        Assert.False(viewModel.Choices[0].Style.UsesCompactTitle);
        Assert.Contains(nameof(CreateNewCompanyPageViewModel.Choices), changed);
    }
}
