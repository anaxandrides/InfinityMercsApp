using Bunit;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.Web.Layout;
using InfinityMercsApp.Web.Pages;
using InfinityMercsApp.Web.Platform;
using Microsoft.AspNetCore.Components;

namespace InfinityMercsApp.Web.UnitTests.Site;

/// <summary>Managing a season on the site: its header, its troopers one at a time, and recruiting.</summary>
public sealed class SeasonScreenTests : SiteContext
{
    public SeasonScreenTests() => Authorization.SetAuthorized("Kestrel");

    private IRenderedComponent<Season> OpenFoundedSeason()
    {
        FoundBlueLions();
        var season = Render<Season>();
        season.WaitForAssertion(() => Assert.NotEmpty(season.FindAll(".team-list .unit-row")), Patience);
        return season;
    }

    [Fact]
    public void Open_FoundedCompany_ShowsItsCaptain()
    {
        var season = OpenFoundedSeason();

        season.WaitForAssertion(() => season.Find(".team-list .unit-row").Click(), Patience);

        season.WaitForAssertion(() => Assert.Equal("Captain", season.Find(".trooper h2").TextContent), Patience);
        Assert.NotEmpty(season.FindAll(".trooper .profile-lines"));
        Assert.Contains("PERKS", season.Markup, StringComparison.Ordinal);
        Assert.Contains("GEAR", season.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Open_ById_FindsTheSeasonStarted()
    {
        OpenFoundedSeason();
        var record = (await Get<SeasonRecordCatalog>().ListAsync()).Single();

        var season = Render<Season>(parameters => parameters.Add(p => p.SeasonId, record.SeasonIdentifier));

        season.WaitForAssertion(() => Assert.Equal("Blue Lions", season.Find("h1").TextContent), Patience);
    }

    [Fact]
    public void Rename_Trooper_AsksForTheNameAndShowsIt()
    {
        var dialogs = Render<DialogHost>();
        var season = OpenFoundedSeason();
        season.WaitForAssertion(() => season.Find(".team-list .unit-row").Click(), Patience);

        season.WaitForAssertion(() => season.FindAll(".trooper-actions button").First(button => button.TextContent.Contains("RENAME", StringComparison.Ordinal)).Click(), Patience);
        dialogs.WaitForElement("input").Input("Hawk");
        dialogs.Find(".dialog").KeyDown("Enter");

        season.WaitForAssertion(() => Assert.Equal("Hawk", season.Find(".trooper h2").TextContent), Patience);
    }

    [Fact]
    public void GearSlot_Picked_KeepsThePick()
    {
        var season = OpenFoundedSeason();
        season.WaitForAssertion(() => season.Find(".team-list .unit-row").Click(), Patience);
        season.WaitForAssertion(() => Assert.NotEmpty(season.FindAll(".gear-slot select")), Patience);

        season.FindAll(".gear-slot select")[0].Change("0");

        Assert.NotEmpty(season.FindAll(".gear-slot"));
    }

    [Fact]
    public void Recruit_Season_OpensTheBuilderToRecruit()
    {
        var season = OpenFoundedSeason();

        season.Find(".season-header button.primary").Click();

        season.WaitForAssertion(() => Assert.EndsWith("/companies/new/build", Get<NavigationManager>().Uri, StringComparison.Ordinal), Patience);
        Assert.True(Get<CompanySelectionSession>().Current?.IsRecruiting);
        var builder = Render<CompanySelection>();
        builder.WaitForAssertion(() => Assert.NotEmpty(builder.FindAll(".credits")), Patience);
    }
}
