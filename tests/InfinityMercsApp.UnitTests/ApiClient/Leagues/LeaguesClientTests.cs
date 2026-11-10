using System.Net;
using InfinityMercsApp.Domain.Models.Leagues;
using InfinityMercsApp.Infrastructure.API.Leagues;
using InfinityMercsApp.UnitTests.ApiClient.UserDocuments;

namespace InfinityMercsApp.UnitTests.ApiClient.Leagues;

public sealed class LeaguesClientTests
{
    private static readonly LeaguesEndpoint Endpoint = new(new Uri("https://api.example/api/"));

    private const string League =
        """{"leagueId":"l1","name":"Spring","currentSeason":2,"season":2,"isOrganizer":true,"inviteCode":"ABC234","members":[{"memberId":"m1","displayName":"Olive","isOrganizer":true,"isYou":true}],"entries":[],"games":[],"enteredSeasonIds":["s1"]}""";

    private static LeaguesClient Client(HttpMessageHandler handler) => new(new HttpClient(handler), Endpoint);

    [Fact]
    public async Task CreateAsync_Created_SendsTheTokenAndBodyAndReadsTheLeague()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, League);

        var reply = await Client(handler).CreateAsync(new NewLeague { Name = "Spring", DisplayName = "Olive" }, "token-1");

        Assert.Equal(LeaguesStatus.Ok, reply.Status);
        Assert.Equal("ABC234", reply.Value?.InviteCode);
        Assert.Equal(["s1"], reply.Value?.EnteredSeasonIds);
        Assert.Equal(HttpMethod.Post, handler.Request?.Method);
        Assert.Equal("https://api.example/api/leagues", handler.Request?.RequestUri?.ToString());
        Assert.Equal("Bearer token-1", handler.Request?.Headers.Authorization?.ToString());
        Assert.Contains("\"displayName\":\"Olive\"", handler.RequestBody);
    }

    [Fact]
    public async Task RecordGameAsync_AnyGame_SendsResultsByName()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, League);
        var game = new NewLeagueGame
        {
            PlayedOn = new DateOnly(2026, 11, 1),
            Sides = [new LeagueGameSide { SeasonId = "s1", Result = GameResult.Draw, ObjectivePoints = 4 }]
        };

        await Client(handler).RecordGameAsync("l1", game, "token-1");

        Assert.Equal("https://api.example/api/leagues/l1/games", handler.Request?.RequestUri?.ToString());
        Assert.Contains("\"result\":\"Draw\"", handler.RequestBody);
        Assert.Contains("\"playedOn\":\"2026-11-01\"", handler.RequestBody);
    }

    [Fact]
    public async Task EnterAsync_AnySeason_PutsTheRenownToTheEntry()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, League);

        await Client(handler).EnterAsync("l1", "s1", new LeagueEntryUpdate { Renown = 321 }, "token-1");

        Assert.Equal(HttpMethod.Put, handler.Request?.Method);
        Assert.Equal("https://api.example/api/leagues/l1/entries/s1", handler.Request?.RequestUri?.ToString());
        Assert.Contains("\"renown\":321", handler.RequestBody);
    }

    [Fact]
    public async Task StartSeasonAsync_AnyLeague_SaysWhichSeasonItStartsFrom()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, League);

        await Client(handler).StartSeasonAsync("l1", 1, "token-1");

        Assert.Equal("https://api.example/api/leagues/l1/seasons?from=1", handler.Request?.RequestUri?.ToString());
    }

    [Fact]
    public async Task JoinAsync_AnyCode_PostsToTheInvite()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, League);

        await Client(handler).JoinAsync("ABC234", new JoinLeague { DisplayName = "Nova" }, "token-1");

        Assert.Equal(HttpMethod.Post, handler.Request?.Method);
        Assert.Equal("https://api.example/api/invites/ABC234", handler.Request?.RequestUri?.ToString());
    }

    [Fact]
    public async Task RecordGameAsync_Rejected_CarriesTheProblems()
    {
        var handler = new CapturingHandler(HttpStatusCode.BadRequest, """["Only one company can win a game."]""");

        var reply = await Client(handler).RecordGameAsync("l1", new NewLeagueGame(), "token-1");

        Assert.Equal(LeaguesStatus.Rejected, reply.Status);
        Assert.Equal(["Only one company can win a game."], reply.Problems);
    }

    [Fact]
    public async Task CreateAsync_RejectedWithoutAList_HasNoProblems()
    {
        var reply = await Client(new CapturingHandler(HttpStatusCode.RequestEntityTooLarge, string.Empty)).CreateAsync(new NewLeague(), "token-1");

        Assert.Equal(LeaguesStatus.Rejected, reply.Status);
        Assert.Empty(reply.Problems);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, LeaguesStatus.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized, LeaguesStatus.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, LeaguesStatus.Forbidden)]
    [InlineData(HttpStatusCode.Conflict, LeaguesStatus.Conflict)]
    [InlineData(HttpStatusCode.InternalServerError, LeaguesStatus.Unavailable)]
    public async Task GetAsync_Status_IsReadAs(HttpStatusCode code, LeaguesStatus expected) =>
        Assert.Equal(expected, (await Client(new CapturingHandler(code, string.Empty)).GetAsync("l1", null, "token-1")).Status);

    [Theory]
    [InlineData(null, "https://api.example/api/leagues/l1")]
    [InlineData(3, "https://api.example/api/leagues/l1?season=3")]
    public async Task GetAsync_Season_AsksForIt(int? season, string expected)
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, League);

        await Client(handler).GetAsync("l1", season, "token-1");

        Assert.Equal(expected, handler.Request?.RequestUri?.ToString());
    }

    [Fact]
    public async Task ListAsync_Unreachable_IsUnavailable() =>
        Assert.Equal(LeaguesStatus.Unavailable, (await Client(new FailingHandler()).ListAsync("token-1")).Status);

    [Fact]
    public async Task EntryFilesAsync_Ok_ReadsTheSeason()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, """{"season":{"companyName":"Red Company","companyIdentifier":"c1","rounds":[]}}""");

        var reply = await Client(handler).EntryFilesAsync("l1", "s1", "token-1");

        Assert.Equal("Red Company", reply.Value?.Season?.CompanyName);
        Assert.Null(reply.Value?.Company);
    }

    [Fact]
    public async Task OtherCalls_AnyLeague_GoToTheirRoutes()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, League);
        var client = Client(handler);
        var routes = new List<string>();

        await client.ListAsync("t");
        routes.Add($"{handler.Request?.Method} {handler.Request?.RequestUri}");
        await client.NewInviteAsync("l1", "t");
        routes.Add($"{handler.Request?.Method} {handler.Request?.RequestUri}");
        await client.CloseInviteAsync("l1", "t");
        routes.Add($"{handler.Request?.Method} {handler.Request?.RequestUri}");
        await client.PreviewInviteAsync("ABC234", "t");
        routes.Add($"{handler.Request?.Method} {handler.Request?.RequestUri}");
        await client.WithdrawAsync("l1", "s1", "t");
        routes.Add($"{handler.Request?.Method} {handler.Request?.RequestUri}");
        await client.RemoveGameAsync("l1", "g1", "t");
        routes.Add($"{handler.Request?.Method} {handler.Request?.RequestUri}");
        await client.SetArmyAsync("l1", new LeagueArmyUpdate { ArmySnapshotId = "snap" }, "t");
        routes.Add($"{handler.Request?.Method} {handler.Request?.RequestUri}");

        Assert.Equal(
            [
                "GET https://api.example/api/me/leagues",
                "POST https://api.example/api/leagues/l1/invite",
                "DELETE https://api.example/api/leagues/l1/invite",
                "GET https://api.example/api/invites/ABC234",
                "DELETE https://api.example/api/leagues/l1/entries/s1",
                "DELETE https://api.example/api/leagues/l1/games/g1",
                "PUT https://api.example/api/leagues/l1/army"
            ],
            routes);
    }
}
