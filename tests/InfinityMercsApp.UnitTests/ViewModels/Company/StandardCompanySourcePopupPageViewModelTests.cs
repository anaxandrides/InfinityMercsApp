using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Company;

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
