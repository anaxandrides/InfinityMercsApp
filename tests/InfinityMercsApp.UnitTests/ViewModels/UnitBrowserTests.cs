using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Domain.Models.Filtering;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Controls;

namespace InfinityMercsApp.UnitTests.ViewModels;

public sealed class UnitDetailTextTests
{
    [Fact]
    public void Summaries()
    {
        Assert.Equal("Equipment: a, B", UnitDetailText.NamedSummary("Equipment", ["B", "a", "b", " "]));
        Assert.Equal("Skills: -", UnitDetailText.NamedSummary("Skills", []));
        Assert.Equal($"x{Environment.NewLine}y", UnitDetailText.JoinOrDash(["x", "y", "X"]));
        Assert.Equal("-", UnitDetailText.JoinOrDash([""]));
    }

    [Theory]
    [InlineData("CCW", true)]
    [InlineData("AP CCW", true)]
    [InlineData("Breaker Pistol", true)]
    [InlineData("Combi Rifle", false)]
    [InlineData("Spitfire", false)]
    public void MeleeWeapons(string weapon, bool melee) => Assert.Equal(melee, UnitDetailText.IsMeleeWeaponName(weapon));

    [Fact]
    public void ConfigurationSkillsKeepWhatALieutenantSkillAdds()
    {
        var entries = UnitDetailText.ConfigurationSkillEntries(
            [(1, "Sniper"), (2, "Lieutenant (+1 Order)"), (3, "Lieutenant"), (4, "sniper"), (5, " ")]);

        Assert.Equal([(0, "+1 Order"), (1, "Sniper")], entries);
    }

    [Fact]
    public void LinksMatchTheWholeNameOrTheNameBeforeItsDetail()
    {
        var names = new Dictionary<int, string> { [1] = "Mimetism", [2] = "Hacker" };
        var links = new Dictionary<int, string> { [1] = "wiki/mimetism" };

        Assert.Equal("wiki/mimetism", UnitDetailText.LinkForDisplayName("mimetism (-6)", names, links));
        Assert.Null(UnitDetailText.LinkForDisplayName("Hacker", names, links));
        Assert.Equal("wiki/mimetism", UnitDetailText.FirstLinkWhere(names, links, n => n.StartsWith('M')));
    }

    [Fact]
    public void MergingDropsAStandaloneLtOrderTheLieutenantSkillAlreadyGives() =>
        Assert.Equal(
            "Courage, Lieutenant (+1 Lt Order)",
            UnitDetailText.MergeSummaryAndUnique("Special Skills: Courage, +1 Lt Order", "Lieutenant (+1 Order)"));

    [Fact]
    public void MergingNothingIsADash() => Assert.Equal("-", UnitDetailText.MergeSummaryAndUnique("Equipment: -", "-"));

    [Theory]
    [InlineData("Killer Hacking Device", null, true)]
    [InlineData(null, "Hackable", true)]
    [InlineData(null, "Non-Hackable", false)]
    [InlineData("Multispectral Visor", "Courage", false)]
    public void Hackable(string? equipment, string? skills, bool expected) =>
        Assert.Equal(expected, UnitDetailText.ContainsHackable(equipment, skills));

    [Theory]
    [InlineData("4-4", 10, 10)]
    [InlineData("10-10", 10, 10)]
    [InlineData("6\"-2\"", 15, 5)]
    [InlineData("15cm-5cm", 15, 5)]
    public void SavedMoveIsReadInCentimetres(string move, int first, int second)
    {
        Assert.True(UnitDetailText.TryParseMoveToCentimeters(move, out var a, out var b));
        Assert.Equal((first, second), (a, b));
    }

    [Fact]
    public void UnreadableMoveIsRejected() => Assert.False(UnitDetailText.TryParseMoveToCentimeters("-", out _, out _));

    [Fact]
    public void CubeNames()
    {
        Assert.True(UnitDetailText.IsCubeName("Cube"));
        Assert.False(UnitDetailText.IsCubeName("Cube 2.0"));
        Assert.True(UnitDetailText.IsCube2Name("CUBE 2.0"));
    }

    [Fact]
    public void SubtitleFallsBackToIds() =>
        Assert.Equal("Line Infantry - 7", UnitDetailText.UnitSubtitle(1, 7, new Dictionary<int, string> { [1] = "Line Infantry" }, new Dictionary<int, string>()));
}

public sealed class UnitDetailViewModelTests
{
    private static readonly int SwissGuardId = Army101Data.UnitNamed("SWISS GUARD").Id;

    private static async Task<UnitDetailViewModel> LoadAsync(bool mercsOnly = false, bool lieutenantOnly = false)
    {
        var detail = new UnitDetailViewModel(Army101Data.Create());
        await detail.LoadAsync(new UnitDetailRequest(Army101Data.PanOceania, SwissGuardId, "Swiss Guard", "logo.svg", mercsOnly, lieutenantOnly));
        return detail;
    }

    [Fact]
    public async Task LoadsTheStatLineSummariesAndConfigurations()
    {
        var detail = await LoadAsync();

        Assert.Equal("Swiss Guard", detail.UnitNameHeading);
        Assert.Equal("logo.svg", detail.LogoSource);
        Assert.NotEqual("-", detail.UnitBs);
        Assert.Contains("-", detail.UnitMov);
        Assert.NotEmpty(detail.Profiles);
        Assert.Equal($"{detail.Profiles.Count} configurations loaded.", detail.ProfilesStatus);
        Assert.StartsWith("Equipment: ", detail.EquipmentSummary);
        Assert.All(detail.Profiles, p => Assert.StartsWith("SWC ", p.SwcDisplay));
    }

    [Fact]
    public async Task LieutenantOnlyListsLieutenantConfigurations()
    {
        var detail = await LoadAsync(lieutenantOnly: true);
        Assert.All(detail.Profiles, p => Assert.True(p.IsLieutenant));
    }

    [Fact]
    public async Task MercsOnlyHidesSwcConfigurations()
    {
        var all = await LoadAsync();
        var mercs = await LoadAsync(mercsOnly: true);

        Assert.True(mercs.Profiles.Count < all.Profiles.Count);
        Assert.All(mercs.Profiles, p => Assert.False(ProfileValues.IsPositiveSwc(p.Swc)));
        Assert.All(mercs.Profiles, p => Assert.Equal(string.Empty, p.SwcDisplay));
    }

    [Fact]
    public async Task ChoosingAConfigurationMergesItIntoTheSummaries()
    {
        var detail = await LoadAsync();
        var profile = detail.Profiles.First(p => p.UniqueEquipment != "-");

        detail.ApplySelectedProfileTopSummaries(profile);

        Assert.All(UnitDetailText.SummaryValues(profile.UniqueEquipment), item => Assert.Contains(item, detail.EquipmentSummary));
    }

    [Fact]
    public async Task ResetReturnsToNoUnit()
    {
        var detail = await LoadAsync();
        detail.Reset();

        Assert.Equal("Select a unit", detail.UnitNameHeading);
        Assert.Empty(detail.Profiles);
        Assert.Equal("-", detail.UnitBs);
        Assert.Null(detail.LogoSource);
    }

    [Fact]
    public void ASavedTrooperOverlaysItsStats()
    {
        var detail = new UnitDetailViewModel(Army101Data.Create());

        detail.ApplySavedUnitSnapshot("Hero", "4-4", "15", "12", "", "13", "1", "0", "VITA", "1", "2", isLieutenant: true);
        detail.ApplyCaptainStatBonuses(new CaptainStatBonuses(1, 2, 0, 0, 0, 0, 0));
        detail.ApplyAugmentStatOverride("ph", "14");

        Assert.Equal(("Hero", "10-10", "16", "14", "14"), (detail.UnitNameHeading, detail.UnitMov, detail.UnitCc, detail.UnitBs, detail.UnitPh));
        Assert.False(detail.ShowRegularOrderIcon);

        detail.ApplyHackableOverrideFromCurrentConfiguration("Hacking Device", null);
        Assert.True(detail.ShowHackableIcon);
    }

    [Fact]
    public async Task SwitchingToCentimetresReformatsMove()
    {
        var detail = new UnitDetailViewModel(Army101Data.Create());
        detail.ApplySavedUnitSnapshot("Hero", "4-2", "", "", "", "", "", "", "", "", "", false);

        detail.ShowUnitsInCentimeters = true;

        Assert.False(detail.ShowUnitsInInches);
        Assert.Equal("10-5", detail.UnitMov);
        await Task.CompletedTask;
    }
}

public sealed class UnitEncyclopediaViewModelTests
{
    private readonly IArmyDataService _data = Army101Data.Create();

    private async Task<UnitEncyclopediaViewModel> OpenAsync()
    {
        var viewModel = new UnitEncyclopediaViewModel(new FakeNavigationService(), _data, new UnitDetailViewModel(_data));
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task ListsFactionsAndFiltersToSectorials()
    {
        var viewModel = await OpenAsync();
        Assert.Equal(["PanOceania", "Military Orders"], viewModel.Factions.Select(f => f.Name));

        viewModel.ShowSectorialEntriesOnly = true;
        Assert.Equal(["Military Orders"], viewModel.Factions.Select(f => f.Name));
        Assert.False(viewModel.ShowAllFactionEntries);

        viewModel.ShowFactionEntriesOnly = true;
        Assert.Equal(["PanOceania"], viewModel.Factions.Select(f => f.Name));
    }

    [Fact]
    public async Task BrowsingStepsFromFactionToUnitToDetailAndBack()
    {
        var viewModel = await OpenAsync();
        Assert.Equal(BrowseStep.Factions, viewModel.Step);

        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Factions[0]);

        Assert.Equal(BrowseStep.Units, viewModel.Step);
        Assert.True(viewModel.Factions[0].IsSelected);
        Assert.NotEmpty(viewModel.Units);
        Assert.Equal($"{viewModel.Units.Count} units loaded.", viewModel.UnitsStatus);
        Assert.NotEqual("-", viewModel.FireteamCoreCount);

        var unit = viewModel.Units.First(u => u.Name == "SWISS GUARD");
        await viewModel.SelectUnitCommand.ExecuteAsync(unit);

        Assert.Equal(BrowseStep.Detail, viewModel.Step);
        Assert.Equal("SWISS GUARD", viewModel.Detail.UnitNameHeading);
        Assert.NotEmpty(viewModel.Detail.Profiles);

        viewModel.BackToUnitsCommand.Execute(null);
        Assert.Equal(BrowseStep.Units, viewModel.Step);
        viewModel.BackToFactionsCommand.Execute(null);
        Assert.Equal(BrowseStep.Factions, viewModel.Step);
    }

    [Fact]
    public async Task ChangingFactionClearsTheUnit()
    {
        var viewModel = await OpenAsync();
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Factions[0]);
        await viewModel.SelectUnitCommand.ExecuteAsync(viewModel.Units[0]);

        viewModel.ShowSectorialEntriesOnly = true;

        Assert.Null(viewModel.SelectedFaction);
        Assert.Null(viewModel.SelectedUnit);
        Assert.Empty(viewModel.Detail.Profiles);
        Assert.Equal(BrowseStep.Factions, viewModel.Step);
    }

    [Fact]
    public async Task TheLieutenantFilterKeepsOnlyUnitsThatCanBeLieutenant()
    {
        var viewModel = await OpenAsync();
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Factions[0]);
        var allUnits = viewModel.Units.Count;

        await viewModel.ApplyUnitFilterCommand.ExecuteAsync(new UnitFilterCriteria { LieutenantOnlyUnits = true });

        Assert.True(viewModel.LieutenantOnlyUnits);
        Assert.InRange(viewModel.Units.Count, 1, allUnits - 1);
    }

    [Fact]
    public async Task FilterOptionsComeFromTheFactionsUnits()
    {
        var viewModel = await OpenAsync();
        Assert.Empty((await viewModel.BuildUnitFilterPopupOptionsAsync()).Weapons);

        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Factions[0]);
        var options = await viewModel.BuildUnitFilterPopupOptionsAsync();

        Assert.NotEmpty(options.Classification);
        Assert.NotEmpty(options.Weapons);
        Assert.True(options.MaxPoints >= 200);
    }

    [Fact]
    public async Task TabsSwitchBetweenUnitsAndFireteams()
    {
        var viewModel = await OpenAsync();
        viewModel.ShowFireteamsCommand.Execute(null);
        Assert.True(viewModel.ShowFireteamsTab);
        viewModel.ShowUnitsCommand.Execute(null);
        Assert.True(viewModel.ShowUnitsTab);
    }
}

public sealed class PerkTesterViewModelTests
{
    [Fact]
    public async Task ChoosingAUnitSelectsItsFirstConfiguration()
    {
        var data = Army101Data.Create();
        var viewModel = new PerkTesterViewModel(new FakeNavigationService(), data, new UnitDetailViewModel(data));
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Factions[0]);

        await viewModel.SelectUnitCommand.ExecuteAsync(viewModel.Units.First(u => u.Name == "SWISS GUARD"));

        Assert.True(viewModel.Detail.Profiles[0].IsSelected);
        Assert.Single(viewModel.Detail.Profiles, p => p.IsSelected);

        viewModel.SelectProfileCommand.Execute(viewModel.Detail.Profiles[^1]);
        Assert.True(viewModel.Detail.Profiles[^1].IsSelected);
        Assert.False(viewModel.Detail.Profiles[0].IsSelected);
    }

    [Fact]
    public void DescribesAPerkNode()
    {
        var nodeId = CompanyPerkCatalog.GetPerkNodeLists().First().Roots.First().Id;

        var perk = PerkTesterViewModel.Describe(nodeId);

        Assert.NotNull(perk);
        Assert.Contains("| Track ", perk.Meta);
        Assert.StartsWith("Gives: ", perk.Granted);
        Assert.Null(PerkTesterViewModel.Describe("no-such-perk"));
        Assert.Equal("Gives: -", PerkTesterViewModel.GrantedText(null));
    }
}
