using InfinityMercsApp.AotSmokeTests.Support;
using InfinityMercsApp.ApiClient.Telemetry;
using InfinityMercsApp.ApiClient.UserDocuments;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Domain.Models.Leagues;
using InfinityMercsApp.Infrastructure.API.Leagues;
using InfinityMercsApp.Infrastructure.Json;

namespace InfinityMercsApp.AotSmokeTests.Smoke;

/// <summary>The account's API as the app calls it: a company fetched, a game recorded in a league, and telemetry sent.</summary>
public static class ApiClientSmoke
{
    private static readonly Uri Api = new("https://api.example/api/");

    public static async Task RunAsync(SmokeChecks checks)
    {
        var documents = new UserDocumentsClient(
            new HttpClient(new CannedHandler("""{"documentId":"c1","revision":2,"content":{"CompanyName":"Blue Lions","CompanyType":"t","CompanyIdentifier":"c1","SourceFactions":[],"Entries":[]}}""")),
            new UserDocumentsEndpoint(Api),
            new UserDocumentTypes(UserDocumentJsonContext.Default));
        var reply = await documents.GetAsync<SavedCompanyFile>(UserDocumentKind.Company, "c1", "token");
        checks.Equal("json: company fetched", "Ok 2 Blue Lions", $"{reply.Status} {reply.Value?.Revision} {reply.Value?.Content?.CompanyName}");

        var leagueHandler = new CannedHandler("""{"leagueId":"l1","name":"Spring","currentSeason":1,"season":1,"enteredSeasonIds":["s1"],"games":[{"gameId":"g1","playedOn":"2026-11-01","sides":[{"seasonId":"s1","result":"Win","objectivePoints":7}]}]}""");
        var league = await new LeaguesClient(new HttpClient(leagueHandler), new LeaguesEndpoint(Api)).RecordGameAsync(
            "l1",
            new NewLeagueGame { PlayedOn = new DateOnly(2026, 11, 1), Sides = [new LeagueGameSide { SeasonId = "s1", Result = GameResult.Win, ObjectivePoints = 7 }] },
            "token");
        var side = league.Value?.Games.FirstOrDefault()?.Sides.FirstOrDefault();
        checks.Equal(
            "json: league game recorded",
            "Ok Spring Win 7 s1",
            $"{league.Status} {league.Value?.Name} {side?.Result} {side?.ObjectivePoints} {league.Value?.EnteredSeasonIds.FirstOrDefault()}");
        checks.True("json: league game body", leagueHandler.Sent?.Contains("\"result\":\"Win\"", StringComparison.Ordinal) == true &&
                                                leagueHandler.Sent.Contains("\"playedOn\":\"2026-11-01\"", StringComparison.Ordinal));

        var handler = new CannedHandler(string.Empty);
        var sent = await new TelemetryApiClient(new HttpClient(handler), new TelemetryEndpoint(Api))
            .SendAsync(new TelemetryBatch { InstallId = "install-1", Events = [new TelemetryEvent { Kind = TelemetryKind.PageShown, Name = "Home" }] }, accessToken: null);
        checks.Equal("json: telemetry sent", TelemetrySendResult.Delivered, sent);
        checks.True("json: telemetry body", handler.Sent?.Contains("\"installId\":\"install-1\"", StringComparison.Ordinal) == true &&
                                              handler.Sent.Contains("\"PageShown\"", StringComparison.Ordinal));
    }
}
