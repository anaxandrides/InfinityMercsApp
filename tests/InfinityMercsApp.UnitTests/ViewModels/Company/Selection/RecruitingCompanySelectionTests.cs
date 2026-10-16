using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Services.CompanySelection.Common;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.Recruitment;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Company.Selection;
using NSubstitute;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

public sealed class RecruitingCompanySelectionTests
{
    private const string CompanyPath = "/records/blue.json";
    private const string SeasonPath = "/records/blue-season.json";
    private const int Credits = 25;

    private readonly SelectionTestKit _kit = new();
    private readonly SeasonFile _season = new() { Rounds = [new SeasonRound { RoundIndex = 1 }] };

    public RecruitingCompanySelectionTests()
    {
        _kit.Dialogs.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _kit.SeasonFiles.UpdateAsync(SeasonPath, Arg.Any<Action<SeasonFile>>()).Returns(call =>
        {
            call.Arg<Action<SeasonFile>>()(_season);
            return true;
        });
    }

    // A vanilla PanOceania company: its captain and, optionally, more of one unit.
    private static SavedCompanyFile Company(int extraUnitId = 0, int extraCount = 0) => new()
    {
        CompanyName = "Blue Team",
        CompanyType = "Standard Company - Vanilla",
        SourceFactions = [new SavedCompanyFaction { FactionId = Army101Data.PanOceania, FactionName = "PanOceania" }],
        Entries =
        [
            new SavedCompanyEntry { EntryIndex = 0, Name = "Captain", IsLieutenant = true, SourceFactionId = Army101Data.PanOceania, SourceUnitId = 1, Cost = 20 },
            .. Enumerable.Range(1, extraCount).Select(index => new SavedCompanyEntry
            {
                EntryIndex = index,
                Name = "Veteran",
                SourceFactionId = Army101Data.PanOceania,
                SourceUnitId = extraUnitId,
                Cost = 0
            })
        ]
    };

    private async Task<StandardCompanySelectionViewModel> OpenAsync(SavedCompanyFile company)
    {
        _kit.CompanyFiles.LoadAsync(CompanyPath).Returns(company);
        var viewModel = new StandardCompanySelectionViewModel(_kit.Services, ArmySourceSelectionMode.VanillaFactions);
        viewModel.PrepareRecruitment(new CompanyRecruitment(CompanyPath, SeasonPath, company, Credits, []));
        return await SelectionTestKit.OpenAsync(viewModel);
    }

    // The first trooper profile that can be hired, after picking a unit that has one.
    private static async Task<ViewerProfileItem> HireableProfileAsync(CompanySelectionViewModel viewModel)
    {
        foreach (var unit in viewModel.Units.Where(unit => unit.IsVisible).ToList())
        {
            await viewModel.SelectUnitFromListCommand.ExecuteAsync(unit);
            if (viewModel.Profiles.FirstOrDefault(profile => profile is { IsVisible: true, IsLieutenantBlocked: false }) is { } profile)
            {
                return profile;
            }
        }

        throw new InvalidOperationException("No unit has a profile that can be hired.");
    }

    [Fact]
    public async Task TheCompanysFactionAndTroopersAreInPlace()
    {
        var viewModel = await OpenAsync(Company());

        Assert.True(viewModel.IsRecruiting);
        Assert.False(viewModel.IsFoundingCompany);
        Assert.Equal("Recruit for Blue Team", viewModel.PageTitle);
        Assert.Equal("HIRE RECRUITS", viewModel.ConfirmText);
        Assert.Equal("Blue Team", viewModel.CompanyName);
        Assert.Equal(Army101Data.PanOceania, viewModel.LeftSlotFaction?.Id);
        Assert.False(viewModel.ShowFactionStrip);
        Assert.NotEmpty(viewModel.Units);
        var captain = Assert.Single(viewModel.MercsCompanyEntries);
        Assert.True(captain is { IsOnRoster: true, IsLieutenant: true });
    }

    [Fact]
    public async Task TheBudgetIsTheCrLeft()
    {
        var viewModel = await OpenAsync(Company());

        Assert.Equal("25 CR LEFT", viewModel.CreditsLeftText);
        Assert.False(viewModel.IsCompanyValid);
        Assert.False(viewModel.StartCompanyCommand.CanExecute(null));
    }

    [Fact]
    public async Task TheFactionsCannotBeChanged()
    {
        var viewModel = await OpenAsync(Company());

        viewModel.ToggleFactionStripCommand.Execute(null);
        viewModel.OpenFactionStripCommand.Execute(null);
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Factions[0]);

        Assert.False(viewModel.ShowFactionStrip);
        Assert.False(viewModel.Factions[0].IsSelected);
    }

    [Fact]
    public async Task TroopersOnTheRosterCannotBeRemoved()
    {
        var viewModel = await OpenAsync(Company());

        viewModel.RemoveMercsCompanyEntryCommand.Execute(viewModel.MercsCompanyEntries[0]);

        Assert.Single(viewModel.MercsCompanyEntries);
    }

    [Fact]
    public async Task TheCaptainLeadsSoNoRecruitCanBeLieutenant()
    {
        var viewModel = await OpenAsync(Company());
        var lieutenantsSeen = 0;

        foreach (var unit in viewModel.Units.Where(unit => unit.IsVisible).ToList())
        {
            await viewModel.SelectUnitFromListCommand.ExecuteAsync(unit);
            var lieutenants = viewModel.Profiles.Where(profile => profile.IsLieutenant).ToList();
            lieutenantsSeen += lieutenants.Count;
            Assert.All(lieutenants, profile => Assert.True(profile.IsLieutenantBlocked));
        }

        Assert.True(lieutenantsSeen > 0);
    }

    [Fact]
    public async Task NothingOverTheCrLeftCanBeHired()
    {
        var viewModel = await OpenAsync(Company());
        var tooDearSeen = 0;

        foreach (var unit in viewModel.Units.Where(unit => unit.IsVisible).ToList())
        {
            await viewModel.SelectUnitFromListCommand.ExecuteAsync(unit);
            var tooDear = viewModel.Profiles.Where(profile => ProfileValues.ParseCost(profile.Cost) > Credits).ToList();
            tooDearSeen += tooDear.Count;
            Assert.All(tooDear, profile => Assert.True(!profile.IsVisible || profile.IsLieutenantBlocked));
        }

        Assert.True(tooDearSeen > 0);
    }

    [Fact]
    public async Task AvaCountsTheTroopersAlreadyHired()
    {
        var first = await OpenAsync(Company());
        await HireableProfileAsync(first);
        var unit = first.SelectedUnit;
        Assert.NotNull(unit);
        var ava = CompanySelectionSharedUtilities.ParseAvaLimit(first.Detail.Stats.Ava);
        Assert.NotNull(ava);

        var full = await OpenAsync(Company(unit.Id, ava.Value));
        await full.SelectUnitFromListCommand.ExecuteAsync(full.Units.First(candidate => candidate.Id == unit.Id));

        Assert.NotEmpty(full.Profiles);
        Assert.All(full.Profiles, profile => Assert.True(profile.IsLieutenantBlocked));
    }

    [Fact]
    public async Task HiringSavesTheRecruitChargesItAndGoesBack()
    {
        var viewModel = await OpenAsync(Company());
        var profile = await HireableProfileAsync(viewModel);
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(profile);
        var recruit = viewModel.MercsCompanyEntries[^1];
        Assert.True(recruit.CanRemove);
        Assert.True(viewModel.IsCompanyValid);
        Assert.Equal($"{Credits - recruit.CostValue} CR LEFT", viewModel.CreditsLeftText);

        await viewModel.StartCompanyCommand.ExecuteAsync(null);

        await _kit.Dialogs.Received(1).ConfirmAsync("Hire Recruits", $"Hire {recruit.Name} for {recruit.CostValue} CR?");
        await _kit.CompanyFiles.Received(1).SaveAsync(CompanyPath, Arg.Is<SavedCompanyFile>(company =>
            company.Entries.Count == 2 && company.Entries[1].EntryIndex == 1 && company.Entries[1].Cost == recruit.CostValue));
        Assert.Equal(recruit.CostValue, Assert.Single(_season.Rounds[0].Marketplace.Transactions).CostCr);
        await _kit.Navigation.Received(1).PopAsync();
        await _kit.CaptainDialog.DidNotReceiveWithAnyArgs().ShowAsync(Arg.Any<InfinityMercsApp.Services.CompanySelection.Captain.CaptainUpgradePopupContext>());
    }

    [Fact]
    public async Task ARecruitCanBeTakenOutAgain()
    {
        var viewModel = await OpenAsync(Company());
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(await HireableProfileAsync(viewModel));

        viewModel.RemoveMercsCompanyEntryCommand.Execute(viewModel.MercsCompanyEntries[^1]);

        Assert.Single(viewModel.MercsCompanyEntries);
        Assert.Equal("25 CR LEFT", viewModel.CreditsLeftText);
    }

    [Fact]
    public async Task AFailedHireSaysSoAndStays()
    {
        _kit.SeasonFiles.UpdateAsync(SeasonPath, Arg.Any<Action<SeasonFile>>()).Returns(false);
        var viewModel = await OpenAsync(Company());
        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(await HireableProfileAsync(viewModel));

        await viewModel.StartCompanyCommand.ExecuteAsync(null);

        await _kit.Dialogs.Received(1).ShowAlertAsync("Recruits Not Hired", "The season file could not be saved, so nobody was hired.");
        await _kit.Navigation.DidNotReceive().PopAsync();
    }

    [Fact]
    public async Task FoundingACompanyStillUsesThePickedStartingPoints()
    {
        var viewModel = await SelectionTestKit.OpenAsync(new StandardCompanySelectionViewModel(_kit.Services, ArmySourceSelectionMode.VanillaFactions));

        viewModel.StartPointsChoice = "90";

        Assert.Equal("90", viewModel.SelectedStartSeasonPoints);
        Assert.Equal(string.Empty, viewModel.CreditsLeftText);
    }
}
