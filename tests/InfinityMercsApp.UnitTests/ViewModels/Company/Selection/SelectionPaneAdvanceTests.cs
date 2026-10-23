using InfinityMercsApp.Presentation.Company.Selection;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Company.Selection.Rules;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

/// <summary>On a compact screen, picking moves on to the next pane: unit, then its profiles, then the company.</summary>
public sealed class SelectionPaneAdvanceTests
{
    private readonly SelectionTestKit _kit = new();

    private async Task<CompanySelectionViewModel> OpenAsync()
    {
        var viewModel = await SelectionTestKit.OpenAsync(new CompanySelectionViewModel(_kit.Services, new StandardCompanyRules(ArmySourceSelectionMode.VanillaFactions)));
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Slots.Factions[0]);
        viewModel.ActivePane = CompanySelectionPane.Units;
        return viewModel;
    }

    [Fact]
    public async Task SelectUnitFromListCommand_Unit_ShowsItsProfiles()
    {
        var viewModel = await OpenAsync();

        await viewModel.SelectUnitFromListCommand.ExecuteAsync(viewModel.UnitList.Units.First(unit => unit.Name == "FUSILIERS"));

        Assert.Equal(CompanySelectionPane.Profile, viewModel.ActivePane);
    }

    [Fact]
    public async Task AddProfileToMercsCompanyCommand_Profile_ShowsTheCompany()
    {
        var viewModel = await OpenAsync();
        await viewModel.SelectUnitFromListCommand.ExecuteAsync(viewModel.UnitList.Units.First(unit => unit.Name == "FUSILIERS"));

        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(viewModel.Preview.Profiles.First(profile => profile.IsVisible));

        Assert.Equal(CompanySelectionPane.Company, viewModel.ActivePane);
    }
}
