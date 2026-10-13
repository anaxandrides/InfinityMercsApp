using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels;

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
}

public sealed class StandardCompanySourcePopupPageViewModelTests
{
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly ICompanySelectionNavigator _companySelection = Substitute.For<ICompanySelectionNavigator>();

    private StandardCompanySourcePopupPageViewModel Create() => new(_navigation, _companySelection);

    [Fact]
    public async Task EachChoiceOpensStandardSelectionWithItsSource()
    {
        var viewModel = Create();

        await viewModel.VanillaFactionSelectedCommand.ExecuteAsync(null);
        await viewModel.TwoSectorialsSelectedCommand.ExecuteAsync(null);

        Received.InOrder(() =>
        {
            _companySelection.OpenAsync(CompanyKind.Standard, ArmySourceSelectionMode.VanillaFactions);
            _companySelection.OpenAsync(CompanyKind.Standard, ArmySourceSelectionMode.Sectorials);
        });
    }

    [Fact]
    public async Task ANavigationFailureIsSwallowed()
    {
        _companySelection.OpenAsync(default).ReturnsForAnyArgs(Task.FromException(new InvalidOperationException("no shell")));

        await Create().TwoSectorialsSelectedCommand.ExecuteAsync(null);
    }

    [Fact]
    public async Task BackReturnsToCompanyCreation()
    {
        await Create().ClosePopupCommand.ExecuteAsync(null);

        await _navigation.Received(1).NavigateToAsync("//CreateNewCompanyPage");
    }
}
