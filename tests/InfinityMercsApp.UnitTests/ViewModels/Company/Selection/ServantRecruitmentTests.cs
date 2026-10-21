using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Company.Selection.Rules;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

public sealed class ServantRecruitmentTests
{
    private readonly SelectionTestKit _kit = new();

    private async Task<CompanySelectionViewModel> OpenAsync()
    {
        var viewModel = await SelectionTestKit.OpenAsync(new CompanySelectionViewModel(_kit.Services, new StandardCompanyRules(ArmySourceSelectionMode.VanillaFactions)));
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Slots.Factions[0]);
        return viewModel;
    }

    // Picks the unit and returns its first profile that has the skill (any profile when skill is null).
    private static async Task<ViewerProfileItem> ProfileAsync(CompanySelectionViewModel viewModel, string unitName, string? skill = null)
    {
        await viewModel.SelectUnitFromListCommand.ExecuteAsync(viewModel.UnitList.Units.First(unit => unit.Name == unitName));
        return viewModel.Preview.Profiles.First(profile => profile.IsVisible && (skill is null || profile.UniqueSkills.Contains(skill, StringComparison.OrdinalIgnoreCase)));
    }

    private static async Task AddAsync(CompanySelectionViewModel viewModel, string unitName, string? skill = null) =>
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(await ProfileAsync(viewModel, unitName, skill));

    [Fact]
    public async Task AddProfileToMercsCompanyCommand_ServantWithoutDoctorOrEngineer_AlertsAndAddsNothing()
    {
        var viewModel = await OpenAsync();

        await AddAsync(viewModel, "PALBOTS");

        Assert.Empty(viewModel.Roster.Entries);
        await _kit.Dialogs.Received(1).ShowAlertAsync("Needs a Doctor or Engineer", Arg.Any<string>());
    }

    [Fact]
    public async Task AddProfileToMercsCompanyCommand_ServantWithOneDoctor_BelongsToItAndFollowsIt()
    {
        var viewModel = await OpenAsync();
        await AddAsync(viewModel, "KAPLAN TACTICAL SERVICES (KTS)", "Doctor");
        await AddAsync(viewModel, "FUSILIERS");

        await AddAsync(viewModel, "PALBOTS");

        Assert.Equal(3, viewModel.Roster.Entries.Count);
        var palbot = viewModel.Roster.Entries[1];
        Assert.Same(viewModel.Roster.Entries[0], palbot.ServantOwner);
        Assert.True(palbot.IsServant);
        await _kit.Dialogs.DidNotReceive().ChooseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>());
    }

    [Fact]
    public async Task AddProfileToMercsCompanyCommand_ServantWithTwoOwners_AsksWhichOne()
    {
        var viewModel = await OpenAsync();
        await AddAsync(viewModel, "KAPLAN TACTICAL SERVICES (KTS)", "Doctor");
        await AddAsync(viewModel, "KARHU Special Group", "Engineer");
        _kit.Dialogs.ChooseAsync(Arg.Any<string>(), "Cancel", Arg.Any<IReadOnlyList<string>>())
            .Returns(call => call.Arg<IReadOnlyList<string>>()[1]);

        await AddAsync(viewModel, "PALBOTS");

        Assert.Equal("KARHU (Engineer)", viewModel.Roster.Entries[2].ServantOwner?.Name);
    }

    [Fact]
    public async Task AddProfileToMercsCompanyCommand_OwnerChoiceCancelled_AddsNothing()
    {
        var viewModel = await OpenAsync();
        await AddAsync(viewModel, "KAPLAN TACTICAL SERVICES (KTS)", "Doctor");
        await AddAsync(viewModel, "KARHU Special Group", "Engineer");

        await AddAsync(viewModel, "PALBOTS");

        Assert.Equal(2, viewModel.Roster.Entries.Count);
    }

    [Fact]
    public async Task RemoveMercsCompanyEntryCommand_ServantsOwner_RemovesTheServantToo()
    {
        var viewModel = await OpenAsync();
        await AddAsync(viewModel, "KAPLAN TACTICAL SERVICES (KTS)", "Doctor");
        await AddAsync(viewModel, "PALBOTS");

        viewModel.RemoveMercsCompanyEntryCommand.Execute(viewModel.Roster.Entries[0]);

        Assert.Empty(viewModel.Roster.Entries);
    }
}
