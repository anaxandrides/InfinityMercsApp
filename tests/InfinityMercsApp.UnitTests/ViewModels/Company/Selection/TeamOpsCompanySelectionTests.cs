using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Company.Selection.Rules;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

/// <summary>
/// A Team-Ops Company is two or three of one army's Team-Ops troopers (MI, HI, SpecBot), each with one upgrade; its
/// captain is the MI or HI picked as it is founded.
/// </summary>
public sealed class TeamOpsCompanySelectionTests
{
    private readonly SelectionTestKit _kit = new();

    public TeamOpsCompanySelectionTests()
    {
        _kit.AddSpecialOperatives();
        // Each list offers its first entry.
        _kit.Dialogs.ChooseAsync(Arg.Any<string>(), "Cancel", Arg.Any<IReadOnlyList<string>>())
            .Returns(call => call.Arg<IReadOnlyList<string>>()[0]);
    }

    private async Task<CompanySelectionViewModel> OpenAsync()
    {
        var viewModel = await SelectionTestKit.OpenAsync(
            new CompanySelectionViewModel(_kit.Services, new TeamOpsCompanyRules(ArmySourceSelectionMode.VanillaFactions)));
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Slots.Factions.First(faction => faction.Id == Army101Data.PanOceania));
        return viewModel;
    }

    // Adds the Team-Ops trooper whose group's configuration is the nth listed (MI, HI, SpecBot).
    private static async Task AddAsync(CompanySelectionViewModel viewModel, int groupId)
    {
        await viewModel.SelectUnitFromListCommand.ExecuteAsync(viewModel.UnitList.Units.Single());
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(viewModel.Preview.Profiles.Single(profile => profile.GroupId == groupId));
    }

    [Fact]
    public async Task LoadUnits_TeamOpsCompany_ListsOnlyTheTeamOpsEntry()
    {
        var viewModel = await OpenAsync();

        Assert.Equal(SpecialOperativesData.TeamOpsName, Assert.Single(viewModel.UnitList.Units).Name);
    }

    [Fact]
    public async Task AddProfile_TeamOpsTrooper_TakesOneUpgrade()
    {
        var viewModel = await OpenAsync();

        await AddAsync(viewModel, groupId: 1);

        var trooper = Assert.Single(viewModel.Roster.Entries);
        Assert.Equal(["Spec-Ops: MOV 15-10, Dodge"], trooper.SpecOpsPurchases);
        Assert.Equal(15, trooper.UnitMoveFirstCm);
        Assert.Contains("Dodge", trooper.SavedSkills);
        Assert.False(trooper.IsLieutenant);
    }

    [Fact]
    public async Task AddProfile_SpecBot_JoinsOnItsOwnProfileAsARemote()
    {
        var viewModel = await OpenAsync();

        await AddAsync(viewModel, groupId: 3);

        var specBot = Assert.Single(viewModel.Roster.Entries);
        Assert.Equal("REM", specBot.UnitTypeCode);
        Assert.Contains("BS 10", specBot.Subtitle);
        Assert.False(specBot.HasPeripheralStatBlock);
    }

    [Fact]
    public async Task SelectUnit_GroupAlreadyRecruited_BlocksOnlyThatGroup()
    {
        var viewModel = await OpenAsync();
        await AddAsync(viewModel, groupId: 1);

        await viewModel.SelectUnitFromListCommand.ExecuteAsync(viewModel.UnitList.Units.Single());

        Assert.True(viewModel.Preview.Profiles.Single(profile => profile.GroupId == 1).IsLieutenantBlocked);
        Assert.False(viewModel.Preview.Profiles.Single(profile => profile.GroupId == 3).IsLieutenantBlocked);
    }

    [Fact]
    public async Task StartBlockedText_OneTrooper_AsksForTwo()
    {
        var viewModel = await OpenAsync();
        await AddAsync(viewModel, groupId: 1);

        Assert.Equal("Choose at least two Team-Ops troopers.", viewModel.StartBlockedText);
        Assert.False(viewModel.IsCompanyValid);
    }

    [Fact]
    public async Task StartBlockedText_TwoTroopers_NeedsNoLieutenantYet()
    {
        _kit.Settings.GetShowUnitsInInches().Returns(false);
        var viewModel = await OpenAsync();
        await AddAsync(viewModel, groupId: 1);
        await AddAsync(viewModel, groupId: 3);

        Assert.Equal(string.Empty, viewModel.StartBlockedText);
    }

    [Fact]
    public async Task PrepareFoundingAsync_MiAndHi_MakesThePickedOneCaptain()
    {
        var viewModel = await OpenAsync();
        await AddAsync(viewModel, groupId: 1);
        await AddAsync(viewModel, groupId: 2);
        _kit.Dialogs.ChooseAsync(Arg.Is<string>(title => title.StartsWith("Who captains")), "Cancel", Arg.Any<IReadOnlyList<string>>())
            .Returns(call => call.Arg<IReadOnlyList<string>>()[1]);

        Assert.True(await new TeamOpsCompanyRules(ArmySourceSelectionMode.VanillaFactions).PrepareFoundingAsync(viewModel));

        Assert.Equal([false, true], viewModel.Roster.Entries.Select(entry => entry.IsLieutenant));
    }

    [Fact]
    public async Task PrepareFoundingAsync_OnlyTheSpecBotLeftToLead_RefusesToFound()
    {
        var viewModel = await OpenAsync();
        await AddAsync(viewModel, groupId: 3);

        Assert.False(await new TeamOpsCompanyRules(ArmySourceSelectionMode.VanillaFactions).PrepareFoundingAsync(viewModel));
        await _kit.Dialogs.Received(1).ShowAlertAsync("Needs a Captain", Arg.Any<string>());
    }

    [Fact]
    public async Task StartCompany_MiAndSpecBot_NamesTheMiAsCaptainWithoutExperience()
    {
        var viewModel = await OpenAsync();
        await AddAsync(viewModel, groupId: 1);
        await AddAsync(viewModel, groupId: 3);
        viewModel.Roster.CompanyName = "Blue Team";

        await viewModel.StartCompanyCommand.ExecuteAsync(null);

        Assert.True(viewModel.Roster.Entries[0].IsLieutenant);
        await _kit.CaptainDialog.Received(1).ShowAsync(Arg.Is<CaptainUpgradePopupContext>(context =>
            context.Unit.Name == viewModel.Roster.Entries[0].Name && context.BaseExperienceOverride == 0));
    }

    [Fact]
    public void FireteamOf_TwoOrThree_IsADuoOrAHaris()
    {
        Assert.Equal(Domain.Models.Army.Fireteams.FireteamType.Duo, TeamOpsCompany.FireteamOf(2));
        Assert.Equal(Domain.Models.Army.Fireteams.FireteamType.Haris, TeamOpsCompany.FireteamOf(3));
        Assert.Null(TeamOpsCompany.FireteamOf(1));
    }
}
