using Bunit;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.Web.Pages.SeasonSteps;

namespace InfinityMercsApp.Web.UnitTests.Site;

/// <summary>The season's steps on the site: rank-up, a bye, loot, and a round from deployment back to the season.</summary>
public sealed class SeasonStepTests : SeasonSiteContext
{

    [Fact]
    public async Task RankUpPage_FromItsAddress_OpensTheSeasonsTroopers()
    {
        var id = (await OpenFoundedSeasonAsync()).Id;

        var page = Render<RankUpPage>(parameters => parameters.Add(p => p.SeasonId, id));

        page.WaitForAssertion(() => Assert.Equal("RANK UP", page.Find("h1").TextContent), Patience);
    }

    [Fact]
    public async Task ByeRoundPage_FromItsAddress_ShowsTheRoundsBye()
    {
        var id = (await OpenFoundedSeasonAsync()).Id;

        var page = Render<ByeRoundPage>(parameters => parameters.Add(p => p.SeasonId, id));

        page.WaitForAssertion(() => Assert.Equal("ROUND 1 BYE", page.Find("h1").TextContent), Patience);
        Assert.NotEmpty(page.FindAll(".check-row"));
    }

    [Fact]
    public async Task ByeRoundPage_ByeTaken_GoesOnToDowntime()
    {
        var season = (await OpenFoundedSeasonAsync()).Page;
        season.FindAll(".season-actions button").First(button => button.TextContent == "TAKE A BYE").Click();
        season.WaitForAssertion(() => Assert.EndsWith("/bye", Address, StringComparison.Ordinal), Patience);
        var page = Render<ByeRoundPage>(parameters => parameters.Add(p => p.SeasonId, IdIn(Address)));
        page.WaitForAssertion(() => page.Find(".check-row input").Change(true), Patience);

        page.WaitForAssertion(() => page.Find(".step-bar button.primary").Click(), Patience);
        Answer();

        page.WaitForAssertion(() => Assert.EndsWith("/downtime", Address, StringComparison.Ordinal), Patience);
    }

    [Fact]
    public async Task LootBoxPage_BoxAdded_ShowsTheDiceToRoll()
    {
        var id = (await OpenFoundedSeasonAsync()).Id;
        var page = Render<LootBoxPage>(parameters => parameters.Add(p => p.SeasonId, id));
        page.WaitForAssertion(() => Assert.Equal("LOOT BOXES", page.Find("h1").TextContent), Patience);

        page.Find("button.secondary").Click();
        Answer();

        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll(".dice-choice")), Patience);
    }

    [Fact]
    public async Task GameModePage_FromItsAddressAlone_GoesBackToTheSeason()
    {
        var id = (await OpenFoundedSeasonAsync()).Id;

        Render<GameModePage>(parameters => parameters.Add(p => p.SeasonId, id));

        Assert.EndsWith($"/season/{id}", Address, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Round_PlayedThrough_ReturnsToTheSeasonWithTheContractRecorded()
    {
        var founded = await OpenFoundedSeasonAsync();
        var season = founded.Page;
        var id = founded.Id;

        // Deployment: the captain always goes.
        season.FindAll(".season-actions button").First(button => button.TextContent == "PLAY ROUND").Click();
        var play = Render<PlayModePage>(parameters => parameters.Add(p => p.SeasonId, id));
        play.WaitForAssertion(() => Assert.Equal("DEPLOYMENT", play.Find("h1").TextContent), Patience);
        play.WaitForAssertion(() => Assert.NotEmpty(play.FindAll(".deploy-tile")), Patience);
        play.Find(".unit-tile").Click();
        play.WaitForAssertion(() => Assert.NotNull(play.Find(".deploy-detail .statline")), Patience);
        play.Find(".step-bar button.primary").Click();
        play.WaitForAssertion(() => Assert.EndsWith("/game", Address, StringComparison.Ordinal), Patience);

        // The game: three rounds, then end it.
        var game = Render<GameModePage>(parameters => parameters.Add(p => p.SeasonId, id));
        game.WaitForAssertion(() => Assert.Equal("Round 1", game.Find("h1").TextContent), Patience);
        game.Find(".orders .order:not([disabled])").Click();
        game.Find(".game-unit-toggle").Click();
        game.WaitForAssertion(() => Assert.NotNull(game.Find(".game-unit-body")), Patience);
        game.FindAll(".game-unit-body button").First(button => button.TextContent.Contains("XP", StringComparison.Ordinal)).Click();
        game.WaitForAssertion(() => Assert.NotNull(game.Find(".dialog.xp-sheet")), Patience);
        game.Find(".dialog.xp-sheet .xp-check input").Change(true);
        game.Find(".dialog.xp-sheet .buttons button").Click();
        game.Find(".game-header button[aria-label='Next round']").Click();
        game.Find(".game-header button[aria-label='Next round']").Click();
        game.WaitForAssertion(() => Assert.Equal("Round 3", game.Find("h1").TextContent), Patience);
        game.Find(".game-header button.danger").Click();
        Dialogs.WaitForAssertion(() => Dialogs.Find(".dialog button.primary").Click(), Patience);
        game.WaitForAssertion(() => Assert.True(Address.EndsWith("/outcome", StringComparison.Ordinal) || Address.EndsWith("/injuries", StringComparison.Ordinal)), Patience);

        if (Address.EndsWith("/injuries", StringComparison.Ordinal))
        {
            var injuries = Render<InjuriesPage>(parameters => parameters.Add(p => p.SeasonId, id));
            injuries.WaitForAssertion(() => Assert.Equal("INJURIES", injuries.Find("h1").TextContent), Patience);
            foreach (var pass in injuries.FindAll(".injury-check .mode-buttons button.secondary:not(.danger)"))
            {
                pass.Click();
            }

            injuries.Find(".step-bar button.primary").Click();
            injuries.WaitForAssertion(() => Assert.EndsWith("/outcome", Address, StringComparison.Ordinal), Patience);
        }

        // The outcome, then the XP.
        var outcome = Render<MissionOutcomePage>(parameters => parameters.Add(p => p.SeasonId, id));
        outcome.WaitForAssertion(() => Assert.Equal("MISSION OUTCOME", outcome.Find("h1").TextContent), Patience);
        outcome.FindAll(".mode-buttons button").First(button => button.TextContent == "VICTORY").Click();
        outcome.Find("select").Change("5");
        outcome.WaitForAssertion(() => outcome.Find(".step-bar button.primary").Click(), Patience);
        outcome.WaitForAssertion(() => Assert.EndsWith("/experience", Address, StringComparison.Ordinal), Patience);

        var experience = Render<ExperiencePage>(parameters => parameters.Add(p => p.SeasonId, id));
        experience.WaitForAssertion(() => Assert.NotEmpty(experience.FindAll(".experience")), Patience);
        experience.Find(".experience .mvp input").Change(true);
        experience.WaitForAssertion(() => experience.Find(".step-bar button.primary").Click(), Patience);
        experience.WaitForAssertion(() => Assert.EndsWith("/downtime", Address, StringComparison.Ordinal), Patience);

        // Downtime: a quiet week (no event), then back to the season.
        var downtime = Render<DowntimePage>(parameters => parameters.Add(p => p.SeasonId, id));
        downtime.WaitForAssertion(() => Assert.Equal("DOWNTIME EVENT", downtime.Find("h1").TextContent), Patience);
        downtime.FindAll(".step-bar button").First(button => button.TextContent == "REAL").Click();
        Dialogs.WaitForElement("input").Input("1");
        Dialogs.Find(".dialog").KeyDown("Enter");
        downtime.WaitForAssertion(() => Assert.True(downtime.FindAll(".event-card").Count > 0), Patience);

        var seasons = (await Get<SeasonRecordCatalog>().ListAsync()).Single();
        var seasonFile = await Get<ISeasonFileService>().LoadAsync(seasons.SeasonFilePath);
        Assert.Single(seasonFile?.Rounds ?? []);
    }

    // The season's id in a step's address: season/{id}/{step}.
    private static string IdIn(string uri)
    {
        var parts = new Uri(uri).AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return Uri.UnescapeDataString(parts[^2]);
    }
}
