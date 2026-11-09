using InfinityMercsApp.Api.Leagues.Changes;
using InfinityMercsApp.Domain.Leagues;
using InfinityMercsApp.Domain.Validation;
using InfinityMercsApp.Domain.Validation.Leagues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace InfinityMercsApp.Api.Leagues;

/// <summary>
/// Leagues, under leagues/ and me/leagues, and joining them, under invites/. Every route needs a sign-in; a user who
/// doesn't belong to a league is told it isn't there.
/// </summary>
public sealed class LeagueFunctions(LeagueEndpoints endpoints, LeagueService leagues, TimeProvider time)
{
    [Function("ListLeagues")]
    public async Task<IActionResult> ListAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "me/leagues")] HttpRequest request,
        CancellationToken cancellationToken) =>
        await endpoints.UserAsync(request, cancellationToken) is { } user
            ? LeagueEndpoints.Json(await leagues.ListAsync(user.UserId, cancellationToken))
            : new UnauthorizedResult();

    [Function("CreateLeague")]
    public async Task<IActionResult> CreateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "leagues")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await endpoints.UserAsync(request, cancellationToken) is not { } user)
        {
            return new UnauthorizedResult();
        }

        var read = await LeagueEndpoints.ReadAsync(request, NewLeagueValidator.Instance, cancellationToken);
        return read.Body is { } body
            ? LeagueEndpoints.Outcome(await leagues.CreateAsync(user.UserId, body, cancellationToken), user.UserId)
            : Refusal(read.Refusal);
    }

    [Function("GetLeague")]
    public async Task<IActionResult> GetAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leagues/{leagueId}")] HttpRequest request,
        string leagueId,
        CancellationToken cancellationToken)
    {
        if (await endpoints.UserAsync(request, cancellationToken) is not { } user)
        {
            return new UnauthorizedResult();
        }

        return DocumentIdentifiers.IsValid(leagueId) && await leagues.GetAsync(leagueId, user.UserId, cancellationToken) is { } league
            ? LeagueEndpoints.Json(LeagueViews.For(league, user.UserId))
            : new NotFoundResult();
    }

    [Function("NewLeagueInvite")]
    public async Task<IActionResult> NewInviteAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "leagues/{leagueId}/invite")] HttpRequest request,
        string leagueId,
        CancellationToken cancellationToken)
    {
        if (await endpoints.UserAsync(request, cancellationToken) is not { } user)
        {
            return new UnauthorizedResult();
        }

        return DocumentIdentifiers.IsValid(leagueId)
            ? LeagueEndpoints.Outcome(await leagues.NewInviteAsync(leagueId, user.UserId, cancellationToken), user.UserId)
            : new NotFoundResult();
    }

    [Function("CloseLeagueInvite")]
    public async Task<IActionResult> CloseInviteAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "leagues/{leagueId}/invite")] HttpRequest request,
        string leagueId,
        CancellationToken cancellationToken)
    {
        if (await endpoints.UserAsync(request, cancellationToken) is not { } user)
        {
            return new UnauthorizedResult();
        }

        return DocumentIdentifiers.IsValid(leagueId)
            ? LeagueEndpoints.Outcome(await leagues.CloseInviteAsync(leagueId, user.UserId, cancellationToken), user.UserId)
            : new NotFoundResult();
    }

    [Function("PreviewLeagueInvite")]
    public async Task<IActionResult> PreviewInviteAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "invites/{code}")] HttpRequest request,
        string code,
        CancellationToken cancellationToken)
    {
        if (await endpoints.UserAsync(request, cancellationToken) is not { } user)
        {
            return new UnauthorizedResult();
        }

        return InviteCodes.Parse(code) is { } parsed && await leagues.PreviewInviteAsync(parsed, user.UserId, cancellationToken) is { } preview
            ? LeagueEndpoints.Json(preview)
            : new NotFoundResult();
    }

    [Function("JoinLeague")]
    public async Task<IActionResult> JoinAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "invites/{code}")] HttpRequest request,
        string code,
        CancellationToken cancellationToken)
    {
        if (await endpoints.UserAsync(request, cancellationToken) is not { } user)
        {
            return new UnauthorizedResult();
        }

        if (InviteCodes.Parse(code) is not { } parsed)
        {
            return new NotFoundResult();
        }

        var read = await LeagueEndpoints.ReadAsync(request, JoinLeagueValidator.Instance, cancellationToken);
        return read.Body is { } body
            ? LeagueEndpoints.Outcome(await leagues.JoinAsync(parsed, user.UserId, body, cancellationToken), user.UserId)
            : Refusal(read.Refusal);
    }

    [Function("EnterLeagueSeason")]
    public async Task<IActionResult> EnterAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "leagues/{leagueId}/entries/{seasonId}")] HttpRequest request,
        string leagueId,
        string seasonId,
        CancellationToken cancellationToken)
    {
        if (await endpoints.UserAsync(request, cancellationToken) is not { } user)
        {
            return new UnauthorizedResult();
        }

        if (!DocumentIdentifiers.IsValid(leagueId) || !DocumentIdentifiers.IsValid(seasonId))
        {
            return new NotFoundResult();
        }

        var read = await LeagueEndpoints.ReadAsync(request, LeagueEntryUpdateValidator.Instance, cancellationToken);
        return read.Body is { } body
            ? LeagueEndpoints.Outcome(await leagues.EnterAsync(leagueId, user.UserId, seasonId, body, cancellationToken), user.UserId)
            : Refusal(read.Refusal);
    }

    [Function("WithdrawLeagueSeason")]
    public async Task<IActionResult> WithdrawAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "leagues/{leagueId}/entries/{seasonId}")] HttpRequest request,
        string leagueId,
        string seasonId,
        CancellationToken cancellationToken)
    {
        if (await endpoints.UserAsync(request, cancellationToken) is not { } user)
        {
            return new UnauthorizedResult();
        }

        return DocumentIdentifiers.IsValid(leagueId) && DocumentIdentifiers.IsValid(seasonId)
            ? LeagueEndpoints.Outcome(await leagues.ApplyAsync(leagueId, user.UserId, new WithdrawChange(seasonId), cancellationToken), user.UserId)
            : new NotFoundResult();
    }

    [Function("GetLeagueEntryFiles")]
    public async Task<IActionResult> EntryFilesAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "leagues/{leagueId}/entries/{seasonId}")] HttpRequest request,
        string leagueId,
        string seasonId,
        CancellationToken cancellationToken)
    {
        if (await endpoints.UserAsync(request, cancellationToken) is not { } user)
        {
            return new UnauthorizedResult();
        }

        return DocumentIdentifiers.IsValid(leagueId) && DocumentIdentifiers.IsValid(seasonId) &&
               await leagues.EntryFilesAsync(leagueId, user.UserId, seasonId, cancellationToken) is { } entryFiles
            ? LeagueEndpoints.Json(entryFiles)
            : new NotFoundResult();
    }

    [Function("RecordLeagueGame")]
    public async Task<IActionResult> RecordGameAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "leagues/{leagueId}/games")] HttpRequest request,
        string leagueId,
        CancellationToken cancellationToken)
    {
        if (await endpoints.UserAsync(request, cancellationToken) is not { } user)
        {
            return new UnauthorizedResult();
        }

        if (!DocumentIdentifiers.IsValid(leagueId))
        {
            return new NotFoundResult();
        }

        var read = await LeagueEndpoints.ReadAsync(request, NewLeagueGameValidator.Instance, cancellationToken);
        if (read.Body is not { } body)
        {
            return Refusal(read.Refusal);
        }

        var change = new RecordGameChange(body, Guid.NewGuid().ToString("N"), time.GetUtcNow());
        return LeagueEndpoints.Outcome(await leagues.ApplyAsync(leagueId, user.UserId, change, cancellationToken), user.UserId);
    }

    [Function("RemoveLeagueGame")]
    public async Task<IActionResult> RemoveGameAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "leagues/{leagueId}/games/{gameId}")] HttpRequest request,
        string leagueId,
        string gameId,
        CancellationToken cancellationToken)
    {
        if (await endpoints.UserAsync(request, cancellationToken) is not { } user)
        {
            return new UnauthorizedResult();
        }

        return DocumentIdentifiers.IsValid(leagueId) && DocumentIdentifiers.IsValid(gameId)
            ? LeagueEndpoints.Outcome(await leagues.ApplyAsync(leagueId, user.UserId, new RemoveGameChange(gameId), cancellationToken), user.UserId)
            : new NotFoundResult();
    }

    [Function("SetLeagueArmy")]
    public async Task<IActionResult> SetArmyAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "leagues/{leagueId}/army")] HttpRequest request,
        string leagueId,
        CancellationToken cancellationToken)
    {
        if (await endpoints.UserAsync(request, cancellationToken) is not { } user)
        {
            return new UnauthorizedResult();
        }

        if (!DocumentIdentifiers.IsValid(leagueId))
        {
            return new NotFoundResult();
        }

        var read = await LeagueEndpoints.ReadAsync(request, LeagueArmyUpdateValidator.Instance, cancellationToken);
        return read.Body is { } body
            ? LeagueEndpoints.Outcome(await leagues.ApplyAsync(leagueId, user.UserId, new ArmyChange(body.ArmySnapshotId), cancellationToken), user.UserId)
            : Refusal(read.Refusal);
    }

    /// <summary>Starts the league's next season; ?from= is the season the organizer saw as current.</summary>
    [Function("StartLeagueSeason")]
    public async Task<IActionResult> StartSeasonAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "leagues/{leagueId}/seasons")] HttpRequest request,
        string leagueId,
        CancellationToken cancellationToken)
    {
        if (await endpoints.UserAsync(request, cancellationToken) is not { } user)
        {
            return new UnauthorizedResult();
        }

        if (!DocumentIdentifiers.IsValid(leagueId))
        {
            return new NotFoundResult();
        }

        if (!int.TryParse(request.Query["from"], out var fromSeason))
        {
            return new BadRequestObjectResult(new List<string> { "Give the season now being played as ?from=." });
        }

        return LeagueEndpoints.Outcome(await leagues.ApplyAsync(leagueId, user.UserId, new NextSeasonChange(fromSeason), cancellationToken), user.UserId);
    }

    private static IActionResult Refusal(IActionResult? refusal) => refusal ?? new BadRequestResult();
}
