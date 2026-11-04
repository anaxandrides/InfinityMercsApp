using Bunit;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.Web.Pages;
using InfinityMercsApp.Web.Pages.SeasonSteps;
using Microsoft.AspNetCore.Components;

namespace InfinityMercsApp.Web.UnitTests.Site;

/// <summary>The parts of the season's pages a plain round doesn't reach: injuries, perks, services, inducements, loot and downtime.</summary>
public sealed class RoundPagesTests : SeasonSiteContext
{
    // Deploys the captain and plays three rounds; the captain is knocked out first when asked.
    private IRenderedComponent<GameModePage> PlayToTheEnd(string id, bool knockOutCaptain)
    {
        var play = Render<PlayModePage>(parameters => parameters.Add(p => p.SeasonId, id));
        play.WaitForAssertion(() => play.Find(".step-bar button.primary").Click(), Patience);
        play.WaitForAssertion(() => Assert.EndsWith("/game", Address, StringComparison.Ordinal), Patience);
        var game = Render<GameModePage>(parameters => parameters.Add(p => p.SeasonId, id));
        game.WaitForAssertion(() => Assert.Equal("Round 1", game.Find("h1").TextContent), Patience);
        if (knockOutCaptain)
        {
            game.WaitForAssertion(() => game.Find(".game-unit-toggle").Click(), Patience);
            for (var wound = 0; wound < 4; wound++)
            {
                game.WaitForAssertion(() => game.FindAll(".game-unit-body button").First(button => button.TextContent == "Take Wound").Click(), Patience);
            }
        }

        game.Find(".game-header button[aria-label='Next round']").Click();
        game.WaitForAssertion(() => Assert.Equal("Round 2", game.Find("h1").TextContent), Patience);
        game.Find(".game-header button[aria-label='Next round']").Click();
        game.WaitForAssertion(() => game.Find(".game-header button.danger").Click(), Patience);
        Answer();
        game.WaitForAssertion(() => Assert.True(Address.EndsWith("/outcome", StringComparison.Ordinal) || Address.EndsWith("/injuries", StringComparison.Ordinal)), Patience);
        return game;
    }

    [Fact]
    public async Task InjuriesPage_CaptainKnockedOut_RollsAndRecordsTheInjury()
    {
        var id = (await OpenFoundedSeasonAsync()).Id;
        var game = PlayToTheEnd(id, knockOutCaptain: true);
        game.WaitForAssertion(() => Assert.EndsWith("/injuries", Address, StringComparison.Ordinal), Patience);

        var injuries = Render<InjuriesPage>(parameters => parameters.Add(p => p.SeasonId, id));
        injuries.WaitForAssertion(() => Assert.NotEmpty(injuries.FindAll(".injury-check")), Patience);
        injuries.Find(".injury-heading button").Click();
        if (injuries.FindAll(".injury-check .mode-buttons button.danger").Count > 0)
        {
            injuries.Find(".injury-check .mode-buttons button.danger").Click();
        }

        injuries.WaitForAssertion(() => Assert.NotEmpty(injuries.FindAll(".injury-check select")), Patience);
        injuries.Find(".injury-check select").Change("0");
        injuries.WaitForAssertion(() => injuries.Find(".step-bar button.primary").Click(), Patience);

        injuries.WaitForAssertion(() => Assert.EndsWith("/outcome", Address, StringComparison.Ordinal), Patience);
    }

    [Fact]
    public async Task InjuriesPage_InjuredCaptainDeployed_ShedsTheInjuryChosen()
    {
        var id = (await OpenFoundedSeasonAsync()).Id;
        await InjureCaptainAsync(Injury.Arms);
        PlayToTheEnd(id, knockOutCaptain: false);
        Assert.EndsWith("/injuries", Address, StringComparison.Ordinal);

        var injuries = Render<InjuriesPage>(parameters => parameters.Add(p => p.SeasonId, id));
        injuries.WaitForAssertion(() => Assert.Contains("RECOVERY", injuries.Markup, StringComparison.Ordinal), Patience);
        injuries.Find(".injury-check select").Change("0");
        injuries.WaitForAssertion(() => injuries.Find(".step-bar button.primary").Click(), Patience);

        injuries.WaitForAssertion(() => Assert.EndsWith("/outcome", Address, StringComparison.Ordinal), Patience);
        Assert.Empty((await SeasonFileAsync())?.Troopers.Values.SelectMany(trooper => trooper.Injuries) ?? []);
    }

    [Fact]
    public async Task GameModePage_WeaponChosen_ShowsEveryModeInFull()
    {
        var id = (await OpenFoundedSeasonAsync()).Id;
        var play = Render<PlayModePage>(parameters => parameters.Add(p => p.SeasonId, id));
        play.WaitForAssertion(() => play.Find(".step-bar button.primary").Click(), Patience);
        play.WaitForAssertion(() => Assert.EndsWith("/game", Address, StringComparison.Ordinal), Patience);
        var game = Render<GameModePage>(parameters => parameters.Add(p => p.SeasonId, id));
        game.WaitForAssertion(() => game.Find(".game-unit-toggle").Click(), Patience);

        game.WaitForAssertion(() => game.Find(".game-weapon:not(.unmatched)").Click(), Patience);

        game.WaitForAssertion(() => Assert.NotEmpty(game.FindAll(".dialog.detail .weapon-card")), Patience);
        game.Find(".dialog.detail .buttons button").Click();
        game.WaitForAssertion(() => Assert.Empty(game.FindAll(".dialog.detail")), Patience);
    }

    [Fact]
    public async Task GameModePage_OrderSpentThenTakenBack_IsLeftAgain()
    {
        var id = (await OpenFoundedSeasonAsync()).Id;
        var play = Render<PlayModePage>(parameters => parameters.Add(p => p.SeasonId, id));
        play.WaitForAssertion(() => play.Find(".step-bar button.primary").Click(), Patience);
        play.WaitForAssertion(() => Assert.EndsWith("/game", Address, StringComparison.Ordinal), Patience);
        var game = Render<GameModePage>(parameters => parameters.Add(p => p.SeasonId, id));
        game.WaitForAssertion(() => Assert.Equal("Round 1", game.Find("h1").TextContent), Patience);
        var left = game.FindAll(".orders:not(.spent-orders) .order-count").Sum(count => int.Parse(count.TextContent, System.Globalization.CultureInfo.InvariantCulture));

        game.Find(".orders:not(.spent-orders) .order:not([disabled])").Click();
        game.WaitForAssertion(() => Assert.NotEmpty(game.FindAll(".spent-orders .order:not([disabled])")), Patience);
        game.Find(".spent-orders .order:not([disabled])").Click();

        game.WaitForAssertion(() => Assert.Equal(left, game.FindAll(".orders:not(.spent-orders) .order-count").Sum(count => int.Parse(count.TextContent, System.Globalization.CultureInfo.InvariantCulture))), Patience);
        Assert.Empty(game.FindAll(".spent-orders .order:not([disabled])"));
    }

    [Fact]
    public async Task InjuriesPage_CheckPassed_GoesOnUninjured()
    {
        var id = (await OpenFoundedSeasonAsync()).Id;
        var game = PlayToTheEnd(id, knockOutCaptain: true);
        game.WaitForAssertion(() => Assert.EndsWith("/injuries", Address, StringComparison.Ordinal), Patience);
        var injuries = Render<InjuriesPage>(parameters => parameters.Add(p => p.SeasonId, id));
        injuries.WaitForAssertion(() => Assert.NotEmpty(injuries.FindAll(".injury-check")), Patience);

        injuries.FindAll(".injury-check .mode-buttons button").First(button => button.TextContent == "PASS").Click();

        injuries.WaitForAssertion(() => Assert.Contains("active", injuries.FindAll(".injury-check .mode-buttons button")[0].ClassName, StringComparison.Ordinal), Patience);
        injuries.Find(".step-bar button.primary").Click();
        injuries.WaitForAssertion(() => Assert.EndsWith("/outcome", Address, StringComparison.Ordinal), Patience);
    }

    [Fact]
    public async Task RankUpPage_TrooperWithTwoPerkPoints_TakesChosenPerksUntilSpent()
    {
        var id = (await OpenFoundedSeasonAsync()).Id;
        await GiveEveryoneXpAsync(20);
        Get<NavigationManager>().NavigateTo($"season/{id}/rank-up");
        var page = Render<RankUpPage>(parameters => parameters.Add(p => p.SeasonId, id));
        page.WaitForAssertion(() => Assert.Contains("2 perk points to spend", page.Markup, StringComparison.Ordinal), Patience);
        page.FindAll(".rank-up .mode-buttons button").First(button => button.TextContent == "CHOOSE").Click();
        page.Find(".rank-up select").Change("2");

        page.WaitForAssertion(() => page.Find(".rank-up .row-card.choice").Click(), Patience);
        page.WaitForAssertion(() => Assert.Contains("1 perk point to spend", page.Markup, StringComparison.Ordinal), Patience);
        page.WaitForAssertion(() => page.Find(".rank-up .row-card.choice").Click(), Patience);

        page.WaitForAssertion(() => Assert.Contains("Every perk point has been spent.", page.Markup, StringComparison.Ordinal), Patience);
        Assert.Equal(2, (await SeasonFileAsync())?.Troopers.Values.Sum(trooper => trooper.Perks.Count));
        page.Find(".step-bar button.primary").Click();
        page.WaitForAssertion(() => Assert.EndsWith($"/season/{id}", Address, StringComparison.Ordinal), Patience);
    }

    [Fact]
    public async Task RankUpPage_Rolled_ShowsTheRoll()
    {
        var id = (await OpenFoundedSeasonAsync()).Id;
        await GiveEveryoneXpAsync(20);
        var page = Render<RankUpPage>(parameters => parameters.Add(p => p.SeasonId, id));
        page.WaitForAssertion(() => Assert.NotNull(page.Find(".rank-up")), Patience);

        page.FindAll(".rank-up .mode-buttons button").First(button => button.TextContent == "ROLL").Click();

        page.WaitForAssertion(() => Assert.True(page.FindAll(".rank-up .caution").Count > 0), Patience);
    }

    [Fact]
    public async Task MarketsTab_MedicalServices_OffersThemForTheTrooperPicked()
    {
        var season = (await OpenFoundedSeasonAsync()).Page;
        season.FindAll(".tabs button").First(button => button.TextContent == "MARKETS").Click();
        season.WaitForAssertion(() => Assert.NotNull(season.Find(".store-picker")), Patience);
        Later(season);

        var services = season.FindAll(".store-picker option").First(option => option.TextContent.Contains("Medical", StringComparison.OrdinalIgnoreCase));
        season.Find(".store-picker").Change(services.GetAttribute("value"));
        season.WaitForAssertion(() => Assert.NotEmpty(season.FindAll(".store-panel .service")), Patience);
        season.Find(".store-panel select").Change("0");

        // A fresh trooper has no injury to treat and no perk to retrain, so those say why they can't be bought.
        season.WaitForAssertion(() => Assert.NotEmpty(season.FindAll(".service .danger")), Patience);
    }

    [Fact]
    public async Task MarketsTab_AdditionalRecruitment_OffersToRecruit()
    {
        var season = (await OpenFoundedSeasonAsync()).Page;
        season.FindAll(".tabs button").First(button => button.TextContent == "MARKETS").Click();
        season.WaitForAssertion(() => Assert.NotNull(season.Find(".store-picker")), Patience);
        Later(season);

        var recruitment = season.FindAll(".store-picker option").First(option => option.TextContent.Contains("Recruit", StringComparison.OrdinalIgnoreCase));
        season.Find(".store-picker").Change(recruitment.GetAttribute("value"));

        season.WaitForAssertion(() => Assert.Contains("ADDITIONAL RECRUITMENT", season.Find(".store-panel").TextContent, StringComparison.Ordinal), Patience);
    }

    [Fact]
    public async Task WarMarketChoice_FactionMarketPicked_IsConfirmed()
    {
        var season = (await OpenFoundedSeasonAsync()).Page;
        season.FindAll(".tabs button").First(button => button.TextContent == "MARKETS").Click();
        season.WaitForAssertion(() => Assert.NotNull(season.Find(".dialog.war-market")), Patience);

        season.Find(".dialog.war-market select").Change("0");
        season.WaitForAssertion(() => Assert.False(season.FindAll(".dialog.war-market button.primary")[0].HasAttribute("disabled")), Patience);
        season.Find(".dialog.war-market button.primary").Click();

        season.WaitForAssertion(() => Assert.Empty(season.FindAll(".dialog.war-market")), Patience);
    }

    [Fact]
    public async Task WarMarketChoice_AfterAContract_RollsForOne()
    {
        var season = (await OpenFoundedSeasonAsync()).Page;
        await GiveEveryoneXpAsync(1);
        season.FindAll(".tabs button").First(button => button.TextContent == "MARKETS").Click();
        season.WaitForAssertion(() => Assert.NotNull(season.Find(".dialog.war-market")), Patience);

        season.WaitForAssertion(() => season.FindAll(".dialog.war-market button").First(button => button.TextContent == "ROLL").Click(), Patience);

        season.WaitForAssertion(() => Assert.NotEqual("—", season.Find(".dialog.war-market .result").TextContent), Patience);
    }

    [Fact]
    public async Task InventoryTab_ItemBought_CanBeSold()
    {
        var season = (await OpenFoundedSeasonAsync()).Page;
        season.FindAll(".tabs button").First(button => button.TextContent == "MARKETS").Click();
        season.WaitForAssertion(() => Assert.NotNull(season.Find(".store-picker")), Patience);
        Later(season);
        var market = season.FindAll(".store-picker option").First(option => option.TextContent.Contains("Base", StringComparison.OrdinalIgnoreCase));
        season.Find(".store-picker").Change(market.GetAttribute("value"));
        season.WaitForAssertion(() => season.FindAll(".market-tab button.quantity.add").First(button => !button.HasAttribute("disabled")).Click(), Patience);
        season.WaitForAssertion(() => season.Find(".cart-bar button").Click(), Patience);
        season.WaitForAssertion(() => Assert.Empty(season.FindAll(".cart-bar")), Patience);

        season.FindAll(".tabs button").First(button => button.TextContent == "INVENTORY").Click();

        season.WaitForAssertion(() => Assert.NotEmpty(season.FindAll(".inventory-tab button.caution")), Patience);
        var resources = season.Find(".season-header .resources").TextContent;
        season.Find(".inventory-tab button.caution").Click();
        Answer();
        season.WaitForAssertion(() => Assert.NotEqual(resources, season.Find(".season-header .resources").TextContent), Patience);
    }

    [Fact]
    public async Task Inducements_Underdog_HiresBuysTokensAndRents()
    {
        var id = (await OpenFoundedSeasonAsync()).Id;
        var play = Render<PlayModePage>(parameters => parameters.Add(p => p.SeasonId, id));
        play.WaitForAssertion(() => Assert.NotNull(play.Find(".renown-row input.field")), Patience);

        play.Find(".renown-row input.field").Input("400");
        play.WaitForAssertion(() => play.FindAll(".deploy-area button").First(button => button.TextContent.StartsWith("INDUCEMENTS", StringComparison.Ordinal)).Click(), Patience);
        play.WaitForAssertion(() => Assert.NotNull(play.Find(".dialog.shop")), Patience);
        play.FindAll(".dialog.shop .quantity.add").Single().Click();
        // The test data has PanOceania only, which fields the Motorized Bounty Hunters in three loadouts.
        play.FindAll(".dialog.shop .row-card.choice").First(offer => offer.TextContent.Contains("Motorized", StringComparison.Ordinal)).Click();
        Answer();
        play.WaitForAssertion(() => Assert.NotEmpty(play.FindAll(".dialog.shop .row-card.bought")), Patience);
        play.FindAll(".dialog.shop .row-card.bought button").First().Click();
        play.FindAll(".dialog.shop .quantity.remove").Single().Click();

        play.FindAll(".dialog.shop .buttons button").First(button => button.TextContent == "DONE").Click();

        play.WaitForAssertion(() => Assert.Empty(play.FindAll(".dialog.shop")), Patience);
    }

    [Fact]
    public async Task Inducements_RentalTaken_CanBeReturned()
    {
        var id = (await OpenFoundedSeasonAsync()).Id;
        var play = Render<PlayModePage>(parameters => parameters.Add(p => p.SeasonId, id));
        play.WaitForAssertion(() => play.Find(".renown-row input.field").Input("400"), Patience);
        play.WaitForAssertion(() => play.FindAll(".deploy-area button").First(button => button.TextContent.StartsWith("INDUCEMENTS", StringComparison.Ordinal)).Click(), Patience);
        play.WaitForAssertion(() => Assert.NotNull(play.Find(".dialog.shop")), Patience);

        var rentals = play.FindAll(".dialog.shop ul.rows")[1];
        rentals.QuerySelectorAll(".row-card.choice").First(offer => !offer.HasAttribute("disabled")).Click();
        Answer();

        play.WaitForAssertion(() => Assert.NotEmpty(play.FindAll(".dialog.shop ul.rows")[1].QuerySelectorAll(".row-card.bought")), Patience);
        play.FindAll(".dialog.shop ul.rows")[1].QuerySelector(".row-card.bought button")?.Click();
        play.WaitForAssertion(() => Assert.Empty(play.FindAll(".dialog.shop ul.rows")[1].QuerySelectorAll(".row-card.bought")), Patience);
    }

    [Fact]
    public async Task LootBoxPage_BoxRolledOut_TakesTheLoot()
    {
        var id = (await OpenFoundedSeasonAsync()).Id;
        var page = Render<LootBoxPage>(parameters => parameters.Add(p => p.SeasonId, id));
        page.WaitForAssertion(() => page.Find("button.secondary").Click(), Patience);
        Answer();
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll(".dice-choice")), Patience);

        for (var roll = 0; roll < 12 && page.FindAll(".mode-buttons button.primary[disabled]").Count > 0; roll++)
        {
            page.WaitForAssertion(() => page.FindAll(".dice-choice").First(choice => !choice.HasAttribute("disabled")).Click(), Patience);
        }

        page.WaitForAssertion(() => page.Find(".mode-buttons button.primary:not([disabled])").Click(), Patience);

        // Taking the loot with dice left asks first.
        page.WaitForAssertion(() => Assert.True(page.FindAll(".row-card .name").Count > 0 || Dialogs.FindAll(".dialog button.primary").Count > 0), Patience);
        AnswerAnyQuestion();
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll(".row-card .name")), Patience);
    }

    [Fact]
    public async Task DowntimePage_EventResolved_GoesBackToBase()
    {
        var id = (await OpenFoundedSeasonAsync()).Id;
        PlayToTheEnd(id, knockOutCaptain: false);
        var downtime = ToDowntime(id, "DEFEAT");

        // Event 1-2, the cop: the captain floors it (a PH test), rolling 1.
        RollReal(downtime, 1);
        downtime.WaitForAssertion(() => downtime.FindAll(".event-card").First(card => card.TextContent.Contains("CHOICE 2", StringComparison.Ordinal)).QuerySelector("button")?.Click(), Patience);
        downtime.WaitForAssertion(() => downtime.Find(".resolution .row-card.choice").Click(), Patience);
        downtime.WaitForAssertion(() => Assert.Contains("Performed by: Captain", downtime.Markup, StringComparison.Ordinal), Patience);
        RollReal(downtime, 1);

        downtime.WaitForAssertion(() => Assert.Contains("OUTCOME", downtime.Markup, StringComparison.Ordinal), Patience);
        downtime.WaitForAssertion(() => downtime.FindAll(".step-bar button").First(button => button.TextContent == "CONTINUE").Click(), Patience);
        downtime.WaitForAssertion(() => Assert.True(Address.EndsWith($"/season/{id}", StringComparison.Ordinal) || Address.EndsWith("/rank-up", StringComparison.Ordinal), Address), Patience);
    }

    [Fact]
    public async Task DowntimePage_QuietWeek_ListsEveryEventToPick()
    {
        var id = (await OpenFoundedSeasonAsync()).Id;
        PlayToTheEnd(id, knockOutCaptain: false);
        var downtime = ToDowntime(id, "VICTORY");

        RollReal(downtime, 19);

        downtime.WaitForAssertion(() => Assert.Contains("SELECT AN EVENT", downtime.Markup, StringComparison.Ordinal), Patience);
        downtime.WaitForAssertion(() => downtime.Find(".rows .row-card.choice").Click(), Patience);
        downtime.WaitForAssertion(() => Assert.True(downtime.FindAll(".event-card").Count > 1), Patience);
    }

    [Fact]
    public async Task DowntimePage_QuietWeekWithNoEvent_IsSettled()
    {
        var id = (await OpenFoundedSeasonAsync()).Id;
        PlayToTheEnd(id, knockOutCaptain: false);
        var downtime = ToDowntime(id, "VICTORY");
        RollReal(downtime, 20);
        downtime.WaitForAssertion(() => Assert.Contains("SELECT AN EVENT", downtime.Markup, StringComparison.Ordinal), Patience);

        downtime.FindAll("button.secondary").First(button => button.TextContent == "NO EVENT").Click();

        downtime.WaitForAssertion(() => Assert.Contains(downtime.FindAll(".step-bar button"), button => button.TextContent == "CONTINUE"), Patience);
    }

    // From the end of the game to downtime: the outcome, then the XP with the captain as MVP.
    private IRenderedComponent<DowntimePage> ToDowntime(string id, string result)
    {
        var outcome = Render<MissionOutcomePage>(parameters => parameters.Add(p => p.SeasonId, id));
        outcome.WaitForAssertion(() => outcome.FindAll(".mode-buttons button").First(button => button.TextContent == result).Click(), Patience);
        outcome.Find("select").Change("2");
        outcome.WaitForAssertion(() => outcome.Find(".step-bar button.primary").Click(), Patience);
        outcome.WaitForAssertion(() => Assert.EndsWith("/experience", Address, StringComparison.Ordinal), Patience);
        var experience = Render<ExperiencePage>(parameters => parameters.Add(p => p.SeasonId, id));
        experience.WaitForAssertion(() => experience.Find(".experience .mvp input").Change(true), Patience);
        experience.WaitForAssertion(() => experience.Find(".step-bar button.primary").Click(), Patience);
        experience.WaitForAssertion(() => Assert.EndsWith("/downtime", Address, StringComparison.Ordinal), Patience);
        var downtime = Render<DowntimePage>(parameters => parameters.Add(p => p.SeasonId, id));
        downtime.WaitForAssertion(() => Assert.Equal("DOWNTIME EVENT", downtime.Find("h1").TextContent), Patience);
        return downtime;
    }

    // Enters a roll made with real dice.
    private void RollReal(IRenderedComponent<DowntimePage> downtime, int roll)
    {
        downtime.WaitForAssertion(() => downtime.FindAll(".step-bar button").First(button => button.TextContent == "REAL").Click(), Patience);
        Dialogs.WaitForElement("input").Input(roll.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Dialogs.Find(".dialog").KeyDown("Enter");
    }

    // The War Market chooser opens with the tab; it can wait.
    private static void Later(IRenderedComponent<Season> season)
    {
        if (season.FindAll(".dialog.war-market").Count > 0)
        {
            season.FindAll(".dialog.war-market button").First(button => button.TextContent == "LATER").Click();
        }
    }
}
