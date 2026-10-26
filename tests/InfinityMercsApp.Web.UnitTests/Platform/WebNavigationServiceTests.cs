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
    private readonly WebNavigationService _service;

    public WebNavigationServiceTests() =>
        _service = new WebNavigationService(_navigation, _js, _query, new WebDialogService { Presenter = _presenter });

    [Fact]
    public async Task NavigateToAsync_KnownRoute_GoesThereHoldingItsParameters()
    {
        var parameters = new Dictionary<string, object> { ["SeasonFilePath"] = "/mercs/s.json" };

        await _service.NavigateToAsync("SeasonPage", parameters);

        Assert.Equal(["https://site.example/season"], _navigation.Visited);
        Assert.Same(parameters, _query.Take());
        Assert.Null(_query.Take());
    }

    [Fact]
    public async Task PushAsync_PageOnlyInTheApp_SaysSoAndStays()
    {
        await _service.PushAsync("GameModePage");

        Assert.Empty(_navigation.Visited);
        Assert.Equal("Only in the App", Assert.Single(_presenter.Shown).Title);
    }

    [Fact]
    public async Task PopToRootAsync_Anywhere_GoesHome()
    {
        await _service.PopToRootAsync();

        Assert.Equal(["https://site.example/"], _navigation.Visited);
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
