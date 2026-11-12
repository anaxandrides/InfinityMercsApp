using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Company.Selection.Rules;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

/// <summary>
/// A Spec-Ops joins a Standard, Inspiring Leader or TAG company only as its captain: it takes the Lieutenant skill,
/// picks two different upgrades from its army's table, and may bring its SpecBot as a peripheral.
/// </summary>
public sealed class SpecOpsCompanySelectionTests
{
    private readonly SelectionTestKit _kit = new();

    public SpecOpsCompanySelectionTests()
    {
        _kit.AddSpecialOperatives();
        // Each list offers its first entry; the SpecBot question is declined.
        _kit.Dialogs.ChooseAsync(Arg.Any<string>(), "Cancel", Arg.Any<IReadOnlyList<string>>())
            .Returns(call => call.Arg<IReadOnlyList<string>>()[0]);
    }

    private async Task<CompanySelectionViewModel> OpenAsync(CompanyRules? rules = null)
    {
        var viewModel = await SelectionTestKit.OpenAsync(
            new CompanySelectionViewModel(_kit.Services, rules ?? new StandardCompanyRules(ArmySourceSelectionMode.VanillaFactions)));
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Slots.Factions[0]);
        return viewModel;
    }

    private static async Task AddSpecOpsAsync(CompanySelectionViewModel viewModel)
    {
        await viewModel.SelectUnitFromListCommand.ExecuteAsync(
            viewModel.UnitList.Units.Single(unit => unit.Name == SpecialOperativesData.SpecOpsName));
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(viewModel.Preview.Profiles.First(profile => profile.IsVisible));
    }

    [Fact]
    public async Task LoadUnits_StandardCompany_ListsTheSpecOpsButNotTheTeamOps()
    {
        var viewModel = await OpenAsync();

        Assert.True(viewModel.UnitList.Units.Single(unit => unit.Name == SpecialOperativesData.SpecOpsName).IsSpecOps);
        Assert.DoesNotContain(viewModel.UnitList.Units, unit => unit.Name == SpecialOperativesData.TeamOpsName);
    }

    [Fact]
    public async Task LoadUnits_ProxyPack_ListsNoSpecOps()
    {
        var viewModel = await OpenAsync(new ProxyPackCompanyRules(ArmySourceSelectionMode.VanillaFactions));

        Assert.DoesNotContain(viewModel.UnitList.Units, unit => unit.IsSpecOps);
    }

    [Fact]
    public async Task SelectUnit_SpecOps_OffersOnlyItsOwnLoadoutsWithoutSwcAsLieutenants()
    {
        var viewModel = await OpenAsync();

        await viewModel.SelectUnitFromListCommand.ExecuteAsync(viewModel.UnitList.Units.Single(unit => unit.Name == SpecialOperativesData.SpecOpsName));

        var profile = Assert.Single(viewModel.Preview.Profiles);
        Assert.True(profile.IsLieutenant);
        Assert.Equal("20", profile.Cost);
    }

    [Fact]
    public async Task AddProfile_SpecOps_JoinsAsCaptainWithTwoDifferentUpgrades()
    {
        var viewModel = await OpenAsync();

        await AddSpecOpsAsync(viewModel);

        var captain = Assert.Single(viewModel.Roster.Entries);
        Assert.True(captain.IsLieutenant);
        Assert.True(captain.IsSpecOps);
        Assert.Equal(["Spec-Ops: +3 BTS", "Spec-Ops: +1 ARM, Dogged"], captain.SpecOpsPurchases);
        Assert.Contains("BTS 6", captain.Subtitle);
        Assert.Contains("ARM 3", captain.Subtitle);
        Assert.Contains("Dogged", captain.SavedSkills);
        await _kit.Dialogs.Received(1).ChooseAsync(Arg.Any<string>(), "Cancel",
            Arg.Is<IReadOnlyList<string>>(labels => !labels.Contains("+3 BTS") && labels.Contains("+1 ARM, Dogged")));
    }

    [Fact]
    public async Task AddProfile_SpecOpsUpgradeCancelled_TakesTheSpecOpsOutAgain()
    {
        _kit.Dialogs.ChooseAsync(Arg.Any<string>(), "Cancel", Arg.Any<IReadOnlyList<string>>()).Returns((string?)null);
        var viewModel = await OpenAsync();

        await AddSpecOpsAsync(viewModel);

        Assert.Empty(viewModel.Roster.Entries);
    }

    [Fact]
    public async Task AddProfile_SpecOpsBringsItsSpecBot_AddsItsCostAndStatBlock()
    {
        _kit.Dialogs.ChooseAsync(Arg.Any<string>(), "No SpecBot", Arg.Any<IReadOnlyList<string>>())
            .Returns(call => call.Arg<IReadOnlyList<string>>()[0]);
        var viewModel = await OpenAsync();

        await AddSpecOpsAsync(viewModel);

        var captain = Assert.Single(viewModel.Roster.Entries);
        Assert.Equal(26, captain.CostValue);
        Assert.True(captain.HasPeripheralStatBlock);
        Assert.Equal("Flash Pulse", captain.SavedPeripheralRangedWeapons);
    }

    [Fact]
    public async Task AddProfile_LieutenantAlreadyChosen_BlocksTheSpecOps()
    {
        var viewModel = await OpenAsync();
        await AddSpecOpsAsync(viewModel);

        await viewModel.SelectUnitFromListCommand.ExecuteAsync(viewModel.UnitList.Units.Single(unit => unit.Name == SpecialOperativesData.SpecOpsName));

        Assert.All(viewModel.Preview.Profiles, profile => Assert.True(profile.IsLieutenantBlocked));
    }
}
