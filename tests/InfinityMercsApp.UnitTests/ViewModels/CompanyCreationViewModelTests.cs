using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views;

namespace InfinityMercsApp.UnitTests.ViewModels;

public sealed class CreateNewCompanyPageViewModelTests
{
    private readonly FakeNavigationService _navigation = new();
    private readonly FakeCompanySelectionNavigator _companySelection = new();

    private CreateNewCompanyPageViewModel Create() => new(_navigation, _companySelection);

    [Fact]
    public async Task StandardOpensTheSourcePopup()
    {
        await Create().OpenStandardCompanyPopupCommand.ExecuteAsync(null);

        Assert.Equal("//StandardCompanySourcePopupPage", Assert.Single(_navigation.Navigations).Route);
        Assert.Empty(_companySelection.Opened);
    }

    [Fact]
    public async Task BackReturnsToTheSeasonHome()
    {
        await Create().NavigateToModeSelectionPageCommand.ExecuteAsync(null);

        Assert.Equal("//MercsSeasonPage", Assert.Single(_navigation.Navigations).Route);
    }

    [Fact]
    public async Task OtherCompaniesOpenTheirSelectionScreens()
    {
        var viewModel = Create();

        await viewModel.OpenCohesiveCompanyPageCommand.ExecuteAsync(null);
        await viewModel.OpenInspiringLeadershipPageCommand.ExecuteAsync(null);
        await viewModel.OpenAirborneCompanyPageCommand.ExecuteAsync(null);
        await viewModel.OpenTAGCompanyPageCommand.ExecuteAsync(null);
        await viewModel.OpenLoneWolfCompanyPopupCommand.ExecuteAsync(null);

        Assert.Equal(
            [
                (CompanyKind.Cohesive, ArmySourceSelectionMode.Sectorials),
                (CompanyKind.Inspiring, ArmySourceSelectionMode.Sectorials),
                (CompanyKind.Airborne, ArmySourceSelectionMode.Sectorials),
                (CompanyKind.Tag, ArmySourceSelectionMode.Sectorials),
                (CompanyKind.LoneWolf, ArmySourceSelectionMode.VanillaFactions)
            ],
            _companySelection.Opened);
        Assert.Empty(_navigation.Navigations);
    }
}

public sealed class StandardCompanySourcePopupPageViewModelTests
{
    private readonly FakeNavigationService _navigation = new();
    private readonly FakeCompanySelectionNavigator _companySelection = new();

    private StandardCompanySourcePopupPageViewModel Create() => new(_navigation, _companySelection);

    [Fact]
    public async Task EachChoiceOpensStandardSelectionWithItsSource()
    {
        var viewModel = Create();

        await viewModel.VanillaFactionSelectedCommand.ExecuteAsync(null);
        await viewModel.TwoSectorialsSelectedCommand.ExecuteAsync(null);

        Assert.Equal(
            [
                (CompanyKind.Standard, ArmySourceSelectionMode.VanillaFactions),
                (CompanyKind.Standard, ArmySourceSelectionMode.Sectorials)
            ],
            _companySelection.Opened);
    }

    [Fact]
    public async Task BackReturnsToCompanyCreation()
    {
        await Create().ClosePopupCommand.ExecuteAsync(null);

        Assert.Equal("//CreateNewCompanyPage", Assert.Single(_navigation.Navigations).Route);
    }
}
