using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Domain.Models.Filtering;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.Views.Controls;
using NSubstitute;

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
            [new(1, "Sniper"), new(2, "Lieutenant (+1 Order)"), new(3, "Lieutenant"), new(4, "sniper"), new(5, " ")]);

        Assert.Equal([new NamedEntry(0, "+1 Order"), new NamedEntry(1, "Sniper")], entries);
    }

    [Fact]
    public void LinkedLinesMergeRepeatsAndKeepTheFirstLinkFound()
    {
        var links = new Dictionary<int, string> { [2] = "wiki/mimetism" };

        var lines = UnitDetailText.LinkedLines([new(1, "Mimetism"), new(2, "mimetism"), new(3, "Hacker"), new(4, " ")], links);

        Assert.Equal([new LinkedText("Mimetism", "wiki/mimetism"), new LinkedText("Hacker", null)], lines);
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

    private static UnitDetailViewModel Load(bool mercsOnly = false, bool lieutenantOnly = false)
    {
        var detail = new UnitDetailViewModel(Army101Data.Create());
        detail.Load(new UnitDetailRequest(Army101Data.PanOceania, SwissGuardId, "Swiss Guard", "logo.svg", mercsOnly, lieutenantOnly));
        return detail;
    }

    [Fact]
    public void LoadsTheStatLineSummariesAndConfigurations()
    {
        var detail = Load();

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
    public void LieutenantOnlyListsLieutenantConfigurations()
    {
        var detail = Load(lieutenantOnly: true);
        Assert.All(detail.Profiles, p => Assert.True(p.IsLieutenant));
    }

    [Fact]
    public void MercsOnlyHidesSwcConfigurations()
    {
        var all = Load();
        var mercs = Load(mercsOnly: true);

        Assert.True(mercs.Profiles.Count < all.Profiles.Count);
        Assert.All(mercs.Profiles, p => Assert.False(ProfileValues.IsPositiveSwc(p.Swc)));
        Assert.All(mercs.Profiles, p => Assert.Equal(string.Empty, p.SwcDisplay));
    }

    [Fact]
    public void ChoosingAConfigurationMergesItIntoTheSummaries()
    {
        var detail = Load();
        var profile = detail.Profiles.First(p => p.UniqueEquipment != "-");

        detail.ApplySelectedProfileTopSummaries(profile);

        Assert.All(UnitDetailText.SummaryValues(profile.UniqueEquipment), item => Assert.Contains(item, detail.EquipmentSummary));
    }

    [Fact]
    public void ResetReturnsToNoUnit()
    {
        var detail = Load();
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
        detail.ApplyAugmentStatOverride(new StatOverride(Stat.Ph, 14));

        Assert.Equal(("Hero", "10-10", "16", "14", "14"), (detail.UnitNameHeading, detail.UnitMov, detail.UnitCc, detail.UnitBs, detail.UnitPh));
        Assert.False(detail.ShowRegularOrderIcon);

        detail.ApplyHackableOverrideFromCurrentConfiguration("Hacking Device", null);
        Assert.True(detail.ShowHackableIcon);
    }

    [Fact]
    public void SwitchingToCentimetresReformatsMove()
    {
        var detail = new UnitDetailViewModel(Army101Data.Create());
        detail.ApplySavedUnitSnapshot("Hero", "4-2", "", "", "", "", "", "", "", "", "", false);

        detail.ShowUnitsInCentimeters = true;

        Assert.False(detail.ShowUnitsInInches);
        Assert.Equal("10-5", detail.UnitMov);
    }
}

public sealed class UnitEncyclopediaViewModelTests
{
    private readonly IArmyDataService _data = Army101Data.Create();

    private async Task<UnitEncyclopediaViewModel> OpenAsync()
    {
        var viewModel = new UnitEncyclopediaViewModel(Substitute.For<INavigationService>(), _data, new UnitDetailViewModel(_data));
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
        viewModel.SelectUnitCommand.Execute(unit);

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
        viewModel.SelectUnitCommand.Execute(viewModel.Units[0]);

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
        Assert.Empty((viewModel.BuildUnitFilterPopupOptions()).Weapons);

        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Factions[0]);
        var options = viewModel.BuildUnitFilterPopupOptions();

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

    [Fact]
    public async Task OpensTheFilterOnTheAppliedFilter()
    {
        var viewModel = await OpenAsync();
        Assert.False(viewModel.IsUnitFilterOpen);

        viewModel.OpenUnitFilterCommand.Execute(null);

        Assert.True(viewModel.IsUnitFilterOpen);
        Assert.Same(viewModel.ActiveUnitFilter, viewModel.UnitFilterRequest!.ExistingCriteria);
        Assert.False(viewModel.UnitFilterRequest.TeamsView);

        viewModel.CloseUnitFilterCommand.Execute(null);
        Assert.False(viewModel.IsUnitFilterOpen);
    }

    [Fact]
    public async Task ApplyingAFilterClosesThePopup()
    {
        var viewModel = await OpenAsync();
        viewModel.OpenUnitFilterCommand.Execute(null);

        await viewModel.ApplyUnitFilterCommand.ExecuteAsync(UnitFilterCriteria.None);

        Assert.Null(viewModel.UnitFilterRequest);
    }

    [Fact]
    public async Task OnAPhoneOnlyTheCurrentStepShows()
    {
        var viewModel = await OpenAsync();
        Assert.True(viewModel.IsFactionsPaneVisible);
        Assert.False(viewModel.IsBrowsePaneVisible || viewModel.IsDetailPaneVisible);

        viewModel.Step = BrowseStep.Units;
        Assert.True(viewModel.IsBrowsePaneVisible);
        Assert.False(viewModel.IsFactionsPaneVisible);

        viewModel.ApplyLayoutCommand.Execute(AdaptiveLayoutMode.Expanded);
        Assert.True(viewModel.IsFactionsPaneVisible && viewModel.IsBrowsePaneVisible && viewModel.IsDetailPaneVisible);
    }
}

public sealed class PerkTesterViewModelTests
{
    [Fact]
    public async Task ChoosingAUnitSelectsItsFirstConfiguration()
    {
        var data = Army101Data.Create();
        var viewModel = new PerkTesterViewModel(Substitute.For<INavigationService>(), data, new UnitDetailViewModel(data));
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Factions[0]);

        viewModel.SelectUnitCommand.Execute(viewModel.Units.First(u => u.Name == "SWISS GUARD"));

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
