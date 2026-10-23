using InfinityMercsApp.Presentation.Company.Selection;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Company.Selection.Rules;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

/// <summary>
/// On a compact screen, picking moves on to the next pane: unit, then its profiles, then the company; and troopers on
/// the roster can be named.
/// </summary>
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

    [Fact]
    public async Task RenameMercsCompanyEntryCommand_NameGiven_NamesTheTrooper()
    {
        var viewModel = await OpenAsync();
        await viewModel.SelectUnitFromListCommand.ExecuteAsync(viewModel.UnitList.Units.First(unit => unit.Name == "FUSILIERS"));
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(viewModel.Preview.Profiles.First(profile => profile.IsVisible));
        var entry = viewModel.Roster.Entries[0];
        _kit.Dialogs.AskTextAsync("Name Trooper", Arg.Any<string>(), string.Empty, Arg.Any<int>()).Returns("Rook");

        await viewModel.RenameMercsCompanyEntryCommand.ExecuteAsync(entry);

        Assert.Equal("Rook", entry.CustomName);
        Assert.True(entry.HasCustomName);
    }
}
