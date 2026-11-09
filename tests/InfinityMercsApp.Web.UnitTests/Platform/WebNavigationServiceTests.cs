using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.Web.Platform;
using Microsoft.JSInterop;
using NSubstitute;

namespace InfinityMercsApp.Web.UnitTests.Platform;

/// <summary>The shared view models' MAUI routes, followed on the site.</summary>
public sealed class WebNavigationServiceTests
{
    private readonly TestNavigation _navigation = new();
    private readonly IJSRuntime _js = Substitute.For<IJSRuntime>();
    private readonly NavigationQuery _query = new();
    private readonly AnsweringPresenter _presenter = new(DialogAnswer.Dismissed);
    private readonly ISeasonFileService _seasonFiles = Substitute.For<ISeasonFileService>();
    private readonly WebNavigationService _service;

    public WebNavigationServiceTests()
    {
        _service = new WebNavigationService(_navigation, _js, _query, new WebDialogService { Presenter = _presenter }, _seasonFiles);
        _seasonFiles.LoadAsync("/mercs/s.json").Returns(new SeasonFile { SeasonIdentifier = "s1" });
    }

    [Fact]
    public async Task NavigateToAsync_KnownRoute_GoesThereHoldingItsParameters()
    {
        var parameters = new Dictionary<string, object> { ["SeasonFilePath"] = "/mercs/s.json" };

        await _service.NavigateToAsync("SeasonPage", parameters);

        Assert.Equal(["https://site.example/season"], _navigation.Visited);
        Assert.Same(parameters, _query.Take());
        Assert.Null(_query.Take());
    }

    [Theory]
    [InlineData("LeaguePage", "https://site.example/leagues/l1")]
    [InlineData("LeagueCompanyPage", "https://site.example/leagues/l1/companies/s1")]
    [InlineData("JoinLeaguePage", "https://site.example/join/ABC234")]
    public async Task PushAsync_LeaguePage_GoesToItsAddress(string route, string address)
    {
        var parameters = NavigationParameters.ForLeague("l1", "s1");
        parameters[NavigationParameters.InviteCode] = "ABC234";

        await _service.PushAsync(route, parameters);

        Assert.Equal([address], _navigation.Visited);
    }

    [Fact]
    public async Task NavigateToAsync_Leagues_GoesToTheList()
    {
        await _service.NavigateToAsync("//LeaguesPage");

        Assert.Equal(["https://site.example/leagues"], _navigation.Visited);
    }

    [Fact]
    public async Task PushAsync_PageOnlyInTheApp_SaysSoAndStays()
    {
        await _service.PushAsync("ArmoryPage");

        Assert.Empty(_navigation.Visited);
        Assert.Equal("Only in the App", Assert.Single(_presenter.Shown).Title);
    }

    [Theory]
    [InlineData("RankUpPage", "rank-up")]
    [InlineData("ByeRoundPage", "bye")]
    [InlineData("LootBoxPage", "loot")]
    [InlineData("PlayModePage", "play")]
    [InlineData("GameModePage", "game")]
    [InlineData("InjuriesPage", "injuries")]
    [InlineData("MissionOutcomePage", "outcome")]
    [InlineData("ExperiencePage", "experience")]
    [InlineData("DowntimePage", "downtime")]
    public async Task PushAsync_SeasonStep_GoesUnderTheSeasonsAddressHoldingItsParameters(string route, string slug)
    {
        var parameters = NavigationParameters.ForCompany("/mercs/c.json", "/mercs/s.json");

        await _service.PushAsync(route, parameters);

        Assert.Equal([$"https://site.example/season/s1/{slug}"], _navigation.Visited);
        Assert.Same(parameters, _query.Take());
    }

    [Fact]
    public async Task PushAsync_SeasonStepWithoutASeasonFile_SaysSoAndStays()
    {
        await _service.PushAsync("RankUpPage", NavigationParameters.ForCompany("/mercs/c.json"));

        Assert.Empty(_navigation.Visited);
        Assert.Equal("Season Not Found", Assert.Single(_presenter.Shown).Title);
        Assert.Null(_query.Take());
    }

    [Fact]
    public async Task PopToRootAsync_OffTheSeasonsSteps_GoesHome()
    {
        await _service.PopToRootAsync();

        Assert.Equal(["https://site.example/"], _navigation.Visited);
    }

    [Fact]
    public async Task PopToRootAsync_OnASeasonStep_GoesBackToTheSeason()
    {
        await _service.PushAsync("DowntimePage", NavigationParameters.ForCompany("/mercs/c.json", "/mercs/s.json"));

        await _service.PopToRootAsync();

        Assert.Equal("https://site.example/season/s1", _navigation.Visited[^1]);
    }

    [Fact]
    public async Task PopToRootAsync_StepOpenedFromItsAddress_GoesBackToTheSeason()
    {
        _navigation.NavigateTo("season/s2/rank-up");

        await _service.PopToRootAsync();

        Assert.Equal("https://site.example/season/s2", _navigation.Visited[^1]);
    }

    [Fact]
    public async Task PopAsync_Anywhere_GoesBackInTheBrowser()
    {
        await _service.PopAsync();

        var call = Assert.Single(_js.ReceivedCalls());
        Assert.Equal("history.back", call.GetArguments()[0]);
    }

    [Fact]
    public async Task InitializeAsync_Always_HasNothingToDo() => await _service.InitializeAsync();
}
