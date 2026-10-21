using InfinityMercsApp.Presentation.Layouts;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Company;

public sealed class CreateNewCompanyPageViewModelTests
{
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly ICompanySelectionNavigator _companySelection = Substitute.For<ICompanySelectionNavigator>();

    private CreateNewCompanyPageViewModel Create() => new(_navigation, _companySelection);

    [Fact]
    public async Task OpenStandardCompanyPopupCommand_Executed_OpensTheSourcePopup()
    {
        await Create().OpenStandardCompanyPopupCommand.ExecuteAsync(null);

        await _navigation.Received(1).NavigateToAsync("//StandardCompanySourcePopupPage");
        await _companySelection.DidNotReceiveWithAnyArgs().OpenAsync(default);
    }

    [Fact]
    public async Task NavigateBackCommand_Executed_ReturnsToTheSeasonHome()
    {
        await Create().NavigateBackCommand.ExecuteAsync(null);

        await _navigation.Received(1).NavigateToAsync("//MercsSeasonPage");
    }

    [Theory]
    [InlineData(nameof(CreateNewCompanyPageViewModel.OpenCohesiveCompanyPageCommand), CompanyKind.Cohesive, ArmySourceSelectionMode.Sectorials)]
    [InlineData(nameof(CreateNewCompanyPageViewModel.OpenInspiringLeadershipPageCommand), CompanyKind.Inspiring, ArmySourceSelectionMode.Sectorials)]
    [InlineData(nameof(CreateNewCompanyPageViewModel.OpenAirborneCompanyPageCommand), CompanyKind.Airborne, ArmySourceSelectionMode.Sectorials)]
    [InlineData(nameof(CreateNewCompanyPageViewModel.OpenTAGCompanyPageCommand), CompanyKind.Tag, ArmySourceSelectionMode.Sectorials)]
    [InlineData(nameof(CreateNewCompanyPageViewModel.OpenProxyPackCompanyPageCommand), CompanyKind.ProxyPack, ArmySourceSelectionMode.VanillaFactions)]
    public async Task OpenCompanyCommands_OtherCompanies_OpenTheirSelectionScreens(string command, CompanyKind kind, ArmySourceSelectionMode mode)
    {
        var viewModel = Create();
        var relayCommand = Assert.IsAssignableFrom<CommunityToolkit.Mvvm.Input.IAsyncRelayCommand>(
            typeof(CreateNewCompanyPageViewModel).GetProperty(command)?.GetValue(viewModel));

        await relayCommand.ExecuteAsync(null);

        await _companySelection.Received(1).OpenAsync(kind, mode);
        await _navigation.DidNotReceiveWithAnyArgs().NavigateToAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>?>());
    }

    [Fact]
    public void Choices_Compact_ListsSixCardsInOneColumn()
    {
        var viewModel = Create();

        Assert.Equal(
            ["Standard Company", "Cohesive Company", "Inspiring Leader", "Airborne Company", "TAG Company", "Proxy Pack"],
            viewModel.Choices.Select(choice => choice.Title));
        Assert.Same(viewModel.OpenProxyPackCompanyPageCommand, viewModel.Choices[5].Command);
        Assert.Equal(new GridCell(5, 0), viewModel.Choices[5].Cell);
        Assert.Equal("*,*,*,*,*,*", viewModel.ChoiceRows);
    }

    [Fact]
    public void ApplyLayoutCommand_Wide_ReflowsTheCards()
    {
        var viewModel = Create();
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.ApplyLayoutCommand.Execute(AdaptiveLayoutMode.Wide);

        Assert.Equal(new GridCell(1, 1), viewModel.Choices[4].Cell);
        Assert.Equal("*,*", viewModel.ChoiceRows);
        Assert.False(viewModel.Choices[0].Style.UsesCompactTitle);
        Assert.Contains(nameof(CreateNewCompanyPageViewModel.Choices), changed);
    }
}
