using System.Text;
using InfinityMercsApp.Api.Auth;
using InfinityMercsApp.Api.Leagues;
using InfinityMercsApp.Api.UserDocuments;
using InfinityMercsApp.Domain.Models.Leagues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using static InfinityMercsApp.Api.UnitTests.Leagues.Leagues;

namespace InfinityMercsApp.Api.UnitTests.Leagues;

public sealed class LeagueFunctionsTests
{
    private readonly IUserAuthenticator _authenticator = Substitute.For<IUserAuthenticator>();
    private readonly InMemoryLeagueStore _store = new();
    private readonly LeagueFunctions _functions;

    public LeagueFunctionsTests()
    {
        SignIn(Member);
        var service = new LeagueService(_store, Substitute.For<IUserDocumentStore>(), new QueuedInviteCodes("NEW234"), TimeProvider.System);
        _functions = new LeagueFunctions(new LeagueEndpoints(_authenticator), service, TimeProvider.System);
        _store.TryCreateAsync(Sample());
        _store.TryCreateInviteAsync(LeagueInvite.For("ABC234", "league-1"));
    }

    private void SignIn(string? userId) =>
        _authenticator.AuthenticateAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>())
            .Returns(userId is null ? null : new SignedInUser(userId));

    private static HttpRequest Request(string? body = null, string? query = null)
    {
        var context = new DefaultHttpContext();
        if (body is not null)
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            context.Request.Body = new MemoryStream(bytes);
            context.Request.ContentLength = bytes.Length;
        }

        if (query is not null)
        {
            context.Request.QueryString = new QueryString(query);
        }

        return context.Request;
    }

    private static T ValueOf<T>(IActionResult result) => Assert.IsType<T>(Assert.IsType<JsonResult>(result).Value);

    [Fact]
    public async Task ListAsync_SignedOut_IsUnauthorized()
    {
        SignIn(null);

        Assert.IsType<UnauthorizedResult>(await _functions.ListAsync(Request(), CancellationToken.None));
    }

    [Fact]
    public async Task ListAsync_Member_ListsTheirLeagues() =>
        Assert.Equal("Spring League", Assert.Single(ValueOf<List<LeagueSummary>>(await _functions.ListAsync(Request(), CancellationToken.None))).Name);

    [Fact]
    public async Task CreateAsync_GoodLeague_ReturnsItWithItsCode()
    {
        var result = await _functions.CreateAsync(Request("""{"name":"Autumn","displayName":"Mo"}"""), CancellationToken.None);

        var view = ValueOf<LeagueView>(result);
        Assert.Equal("Autumn", view.Name);
        Assert.Equal("NEW234", view.InviteCode);
        Assert.True(view.IsOrganizer);
    }

    [Fact]
    public async Task CreateAsync_NoName_IsABadRequest() =>
        Assert.IsType<BadRequestObjectResult>(await _functions.CreateAsync(Request("""{"name":" ","displayName":"Mo"}"""), CancellationToken.None));

    [Fact]
    public async Task CreateAsync_NotJson_IsABadRequest() =>
        Assert.IsType<BadRequestObjectResult>(await _functions.CreateAsync(Request("league please"), CancellationToken.None));

    [Fact]
    public async Task CreateAsync_HugeBody_IsTooLarge()
    {
        var result = await _functions.CreateAsync(Request(new string(' ', LeagueEndpoints.MaxBodyBytes + 1)), CancellationToken.None);

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, Assert.IsType<StatusCodeResult>(result).StatusCode);
    }

    [Fact]
    public async Task GetAsync_Outsider_IsNotFound()
    {
        SignIn(Outsider);

        Assert.IsType<NotFoundResult>(await _functions.GetAsync(Request(), "league-1", CancellationToken.None));
    }

    [Fact]
    public async Task GetAsync_BadId_IsNotFound() =>
        Assert.IsType<NotFoundResult>(await _functions.GetAsync(Request(), "league 1", CancellationToken.None));

    [Fact]
    public async Task PreviewInviteAsync_TypedLowerCaseWithADash_FindsTheLeague()
    {
        SignIn(Outsider);

        var preview = ValueOf<LeagueInvitePreview>(await _functions.PreviewInviteAsync(Request(), "abc-234", CancellationToken.None));

        Assert.Equal("league-1", preview.LeagueId);
    }

    [Fact]
    public async Task JoinAsync_GoodCode_JoinsAndShowsTheLeague()
    {
        SignIn(Outsider);

        var view = ValueOf<LeagueView>(await _functions.JoinAsync(Request("""{"displayName":"Nova"}"""), "ABC234", CancellationToken.None));

        Assert.Equal("Nova", Assert.Single(view.Members, member => member.IsYou).DisplayName);
    }

    [Fact]
    public async Task JoinAsync_UnknownCode_IsNotFound() =>
        Assert.IsType<NotFoundResult>(await _functions.JoinAsync(Request("""{"displayName":"Nova"}"""), "XYZ789", CancellationToken.None));

    [Fact]
    public async Task NewInviteAsync_Member_IsForbidden()
    {
        var result = await _functions.NewInviteAsync(Request(), "league-1", CancellationToken.None);

        Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<StatusCodeResult>(result).StatusCode);
    }

    [Fact]
    public async Task CloseInviteAsync_Organizer_ClosesJoining()
    {
        SignIn(Organizer);

        Assert.Null(ValueOf<LeagueView>(await _functions.CloseInviteAsync(Request(), "league-1", CancellationToken.None)).InviteCode);
    }

    [Fact]
    public async Task RecordGameAsync_PlayerInTheGame_RecordsIt()
    {
        var body = """{"playedOn":"2026-11-01","mission":"Supplies","sides":[{"seasonId":"season-o","result":"Win","objectivePoints":7},{"seasonId":"season-m","result":"Loss","objectivePoints":3}]}""";

        var view = ValueOf<LeagueView>(await _functions.RecordGameAsync(Request(body), "league-1", CancellationToken.None));

        Assert.Equal("Supplies", Assert.Single(view.Games).Mission);
    }

    [Fact]
    public async Task RecordGameAsync_TwoWinners_IsABadRequest()
    {
        var body = """{"playedOn":"2026-11-01","mission":"","sides":[{"seasonId":"season-o","result":"Win","objectivePoints":7},{"seasonId":"season-m","result":"Win","objectivePoints":3}]}""";

        Assert.IsType<BadRequestObjectResult>(await _functions.RecordGameAsync(Request(body), "league-1", CancellationToken.None));
    }

    [Fact]
    public async Task RemoveGameAsync_NoSuchGame_IsNotFound() =>
        Assert.IsType<NotFoundResult>(await _functions.RemoveGameAsync(Request(), "league-1", "g1", CancellationToken.None));

    [Fact]
    public async Task WithdrawAsync_OwnSeason_TakesItOut() =>
        Assert.DoesNotContain(
            ValueOf<LeagueView>(await _functions.WithdrawAsync(Request(), "league-1", MemberSeason, CancellationToken.None)).Entries,
            entry => entry.SeasonId == MemberSeason);

    [Fact]
    public async Task EnterAsync_NegativeRenown_IsABadRequest() =>
        Assert.IsType<BadRequestObjectResult>(await _functions.EnterAsync(Request("""{"renown":-1}"""), "league-1", MemberSeason, CancellationToken.None));

    [Fact]
    public async Task EnterAsync_SeasonNotOnline_IsABadRequest() =>
        Assert.IsType<BadRequestObjectResult>(await _functions.EnterAsync(Request("""{"renown":10}"""), "league-1", "season-new", CancellationToken.None));

    [Fact]
    public async Task EntryFilesAsync_NotEntered_IsNotFound() =>
        Assert.IsType<NotFoundResult>(await _functions.EntryFilesAsync(Request(), "league-1", "season-x", CancellationToken.None));

    [Fact]
    public async Task SetArmyAsync_Organizer_MovesTheLeague()
    {
        SignIn(Organizer);

        var view = ValueOf<LeagueView>(await _functions.SetArmyAsync(Request("""{"armySnapshotId":"snap-2"}"""), "league-1", CancellationToken.None));

        Assert.Equal("snap-2", view.ArmySnapshotId);
    }

    [Fact]
    public async Task StartSeasonAsync_Organizer_StartsTheNext()
    {
        SignIn(Organizer);

        var view = ValueOf<LeagueView>(await _functions.StartSeasonAsync(Request(query: "?from=1"), "league-1", CancellationToken.None));

        Assert.Equal(2, view.CurrentSeason);
    }

    [Fact]
    public async Task StartSeasonAsync_NoFrom_IsABadRequest()
    {
        SignIn(Organizer);

        Assert.IsType<BadRequestObjectResult>(await _functions.StartSeasonAsync(Request(), "league-1", CancellationToken.None));
    }
}
