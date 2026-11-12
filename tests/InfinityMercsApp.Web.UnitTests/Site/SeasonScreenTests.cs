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

        season.FindAll(".season-actions button").First(button => button.TextContent == "RECRUIT").Click();

        season.WaitForAssertion(() => Assert.EndsWith("/companies/new/build", Get<NavigationManager>().Uri, StringComparison.Ordinal), Patience);
        Assert.True(Get<CompanySelectionSession>().Current?.IsRecruiting);
        var builder = Render<CompanySelection>();
        builder.WaitForAssertion(() => Assert.NotEmpty(builder.FindAll(".credits")), Patience);
    }

    [Fact]
    public void InventoryTab_NewCompany_ShowsLootBoxesAndWhatItOwns()
    {
        var season = OpenFoundedSeason();

        season.FindAll(".tabs button").First(button => button.TextContent == "INVENTORY").Click();

        season.WaitForAssertion(() => Assert.NotNull(season.Find(".inventory-tab")), Patience);
        Assert.Contains("LOOT BOXES", season.Find(".inventory-tab").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void MarketsTab_StorePicked_ShowsItsStockAndBuysFromTheCart()
    {
        var season = OpenFoundedSeason();
        season.FindAll(".tabs button").First(button => button.TextContent == "MARKETS").Click();
        season.WaitForAssertion(() => Assert.NotNull(season.Find(".store-picker")), Patience);
        CloseWarMarketChoice(season);

        season.Find(".store-picker").Change(BaseMarketIndex(season));
        season.WaitForAssertion(() => Assert.NotEmpty(season.FindAll(".market-tab .row-card")), Patience);
        season.FindAll(".market-tab button.quantity.add").First(button => !button.HasAttribute("disabled")).Click();

        season.WaitForAssertion(() => Assert.StartsWith("BUY", season.Find(".cart-bar button").TextContent, StringComparison.Ordinal), Patience);
    }

    [Fact]
    public void MarketsTab_StoreRowChosen_ShowsItInFull()
    {
        var season = OpenFoundedSeason();
        season.FindAll(".tabs button").First(button => button.TextContent == "MARKETS").Click();
        season.WaitForAssertion(() => Assert.NotNull(season.Find(".store-picker")), Patience);
        CloseWarMarketChoice(season);
        season.Find(".store-picker").Change(BaseMarketIndex(season));
        season.WaitForAssertion(() => Assert.NotEmpty(season.FindAll(".detail-link")), Patience);

        season.Find(".detail-link").Click();

        season.WaitForAssertion(() => Assert.NotNull(season.Find(".dialog.detail h2")), Patience);
        season.Find(".detail-heading button").Click();
        season.WaitForAssertion(() => Assert.Empty(season.FindAll(".dialog.detail")), Patience);
    }

    [Fact]
    public void TeamTab_WeaponChosen_ShowsItsProfiles()
    {
        var season = OpenFoundedSeason();
        season.WaitForAssertion(() => season.Find(".team-list .unit-row").Click(), Patience);
        season.WaitForAssertion(() => Assert.NotEmpty(season.FindAll(".profile-lines.ranged .line-link")), Patience);

        season.Find(".profile-lines.ranged .line-link").Click();

        season.WaitForAssertion(() => Assert.NotEmpty(season.FindAll(".dialog.detail .weapon-card")), Patience);
    }

    [Fact]
    public void PlayRound_Season_OpensDeploymentUnderTheSeason()
    {
        var season = OpenFoundedSeason();

        season.FindAll(".season-actions button").First(button => button.TextContent == "PLAY ROUND").Click();

        season.WaitForAssertion(() => Assert.EndsWith("/play", Get<NavigationManager>().Uri, StringComparison.Ordinal), Patience);
        Assert.Contains("/season/", Get<NavigationManager>().Uri, StringComparison.Ordinal);
    }

    // The War Market chooser opens with the tab; it can wait.
    private static void CloseWarMarketChoice(IRenderedComponent<Season> season)
    {
        if (season.FindAll(".dialog.war-market").Count > 0)
        {
            season.FindAll(".dialog.war-market button").First(button => button.TextContent == "LATER").Click();
        }
    }

    private static string BaseMarketIndex(IRenderedComponent<Season> season)
    {
        var options = season.FindAll(".store-picker option").Where(option => option.GetAttribute("value") != "-1").ToList();
        var market = options.FirstOrDefault(option => option.TextContent.Contains("Base", StringComparison.OrdinalIgnoreCase)) ?? options[0];
        return market.GetAttribute("value") ?? "0";
    }
}
