using Bunit;
using Bunit.TestDoubles;
using InfinityMercsApp.Web.Layout;
using InfinityMercsApp.Web.Pages;
using Microsoft.AspNetCore.Components;

namespace InfinityMercsApp.Web.UnitTests.Site;

/// <summary>Founding a company on the site, through the captain's configuration, into its first season.</summary>
public sealed class FoundingTests : SiteContext
{
    public FoundingTests() => Authorization.SetAuthorized("Kestrel");

    private BunitNavigationManager Navigation => (BunitNavigationManager)Get<NavigationManager>();

    [Fact]
    public void Found_LieutenantHired_ConfiguresTheCaptainAndOpensTheSeason()
    {
        var captainHost = Render<CaptainHost>();
        var page = BuildPanOceania();
        HireLieutenant(page);
        page.Find("#company-name").Input("Blue Lions");

        page.WaitForAssertion(() => Assert.False(page.Find(".start button.primary").HasAttribute("disabled")), Patience);
        page.Find(".start button.primary").Click();

        captainHost.WaitForAssertion(() => Assert.NotEmpty(captainHost.FindAll(".captain")), Patience);
        Assert.Contains("lieutenant", captainHost.Markup, StringComparison.Ordinal);
        captainHost.FindAll(".captain-name button")[0].Click();
        captainHost.Find(".captain-name input").Input("Kestrel");
        captainHost.Find(".captain-name button.primary").Click();
        Assert.Equal("Kestrel", captainHost.Find(".captain-name .title").TextContent);
        captainHost.FindAll(".stat-upgrades select")[0].Change("1");
        Assert.NotEmpty(captainHost.FindAll(".statline td.modified"));
        captainHost.Find(".buttons button.primary").Click();

        page.WaitForAssertion(() => Assert.EndsWith("/season", Navigation.Uri, StringComparison.Ordinal), Patience);
        captainHost.WaitForAssertion(() => Assert.Empty(captainHost.FindAll(".captain")), Patience);

        var season = Render<Season>();
        season.WaitForAssertion(() => Assert.Equal("Blue Lions", season.Find("h1").TextContent), Patience);
        Assert.NotEmpty(season.FindAll(".renown"));
        Assert.Contains("/season/", Navigation.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void Found_CaptainBackedOut_StaysOnTheBuilder()
    {
        var captainHost = Render<CaptainHost>();
        var page = BuildPanOceania();
        HireLieutenant(page);
        page.Find("#company-name").Input("Blue Lions");

        page.WaitForAssertion(() => page.Find(".start button.primary").Click(), Patience);
        captainHost.WaitForAssertion(() => captainHost.Find(".buttons button.secondary").Click(), Patience);

        captainHost.WaitForAssertion(() => Assert.Empty(captainHost.FindAll(".captain")), Patience);
        Assert.DoesNotContain("season", Navigation.Uri, StringComparison.Ordinal);
        Assert.Single(page.FindAll(".entry-card"));
    }

    [Fact]
    public void Season_NothingToOpen_SaysNotFound()
    {
        var season = Render<Season>();

        season.WaitForAssertion(() => Assert.Equal("Season not found", season.Find("h1").TextContent), Patience);
    }

    [Fact]
    public void Season_UnknownId_SaysNotFound()
    {
        var season = Render<Season>(parameters => parameters.Add(p => p.SeasonId, "missing"));

        season.WaitForAssertion(() => Assert.Equal("Season not found", season.Find("h1").TextContent), Patience);
    }

    [Fact]
    public void Trooper_RenamedThenRemoved_FollowsTheRoster()
    {
        var dialogs = Render<DialogHost>();
        var page = BuildPanOceania();
        HireLieutenant(page);

        page.WaitForAssertion(() => page.FindAll(".entry-actions button").First(button => button.TextContent == "Rename").Click(), Patience);
        dialogs.WaitForElement("input").Input("Hawk");
        dialogs.Find(".dialog").KeyDown("Enter");
        page.WaitForAssertion(() => Assert.Contains("Hawk", page.Find(".entry-card .custom-name").TextContent, StringComparison.Ordinal), Patience);

        page.FindAll(".entry-actions button").First(button => button.TextContent == "Remove").Click();
        page.WaitForAssertion(() => Assert.Empty(page.FindAll(".entry-card")), Patience);
        Assert.Contains("Pick a unit", page.Find(".roster-pane").TextContent, StringComparison.Ordinal);
    }
}
