using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using InfinityMercsApp.ApiClient;
using InfinityMercsApp.Domain.Models.Leagues;

namespace InfinityMercsApp.Infrastructure.API.Leagues;

/// <inheritdoc/>
public sealed class LeaguesClient(HttpClient httpClient, LeaguesEndpoint endpoint) : ILeaguesApi
{
    private static readonly LeaguesJsonContext Json = LeaguesJsonContext.Default;

    /// <inheritdoc/>
    public Task<LeaguesReply<List<LeagueSummary>>> ListAsync(string accessToken, CancellationToken cancellationToken = default) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Get, endpoint.Mine), Json.ListLeagueSummary, accessToken, cancellationToken);

    /// <inheritdoc/>
    public Task<LeaguesReply<LeagueView>> CreateAsync(NewLeague league, string accessToken, CancellationToken cancellationToken = default) =>
        SendAsync(With(HttpMethod.Post, endpoint.Leagues, league, Json.NewLeague), Json.LeagueView, accessToken, cancellationToken);

    /// <inheritdoc/>
    public Task<LeaguesReply<LeagueView>> GetAsync(string leagueId, string accessToken, CancellationToken cancellationToken = default) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Get, endpoint.League(leagueId)), Json.LeagueView, accessToken, cancellationToken);

    /// <inheritdoc/>
    public Task<LeaguesReply<LeagueView>> NewInviteAsync(string leagueId, string accessToken, CancellationToken cancellationToken = default) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Post, endpoint.Invite(leagueId)), Json.LeagueView, accessToken, cancellationToken);

    /// <inheritdoc/>
    public Task<LeaguesReply<LeagueView>> CloseInviteAsync(string leagueId, string accessToken, CancellationToken cancellationToken = default) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Delete, endpoint.Invite(leagueId)), Json.LeagueView, accessToken, cancellationToken);

    /// <inheritdoc/>
    public Task<LeaguesReply<LeagueInvitePreview>> PreviewInviteAsync(string code, string accessToken, CancellationToken cancellationToken = default) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Get, endpoint.InviteCode(code)), Json.LeagueInvitePreview, accessToken, cancellationToken);

    /// <inheritdoc/>
    public Task<LeaguesReply<LeagueView>> JoinAsync(string code, JoinLeague join, string accessToken, CancellationToken cancellationToken = default) =>
        SendAsync(With(HttpMethod.Post, endpoint.InviteCode(code), join, Json.JoinLeague), Json.LeagueView, accessToken, cancellationToken);

    /// <inheritdoc/>
    public Task<LeaguesReply<LeagueView>> EnterAsync(string leagueId, string seasonId, LeagueEntryUpdate update, string accessToken, CancellationToken cancellationToken = default) =>
        SendAsync(With(HttpMethod.Put, endpoint.Entry(leagueId, seasonId), update, Json.LeagueEntryUpdate), Json.LeagueView, accessToken, cancellationToken);

    /// <inheritdoc/>
    public Task<LeaguesReply<LeagueView>> WithdrawAsync(string leagueId, string seasonId, string accessToken, CancellationToken cancellationToken = default) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Delete, endpoint.Entry(leagueId, seasonId)), Json.LeagueView, accessToken, cancellationToken);

    /// <inheritdoc/>
    public Task<LeaguesReply<LeagueEntryFiles>> EntryFilesAsync(string leagueId, string seasonId, string accessToken, CancellationToken cancellationToken = default) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Get, endpoint.Entry(leagueId, seasonId)), Json.LeagueEntryFiles, accessToken, cancellationToken);

    /// <inheritdoc/>
    public Task<LeaguesReply<LeagueView>> RecordGameAsync(string leagueId, NewLeagueGame game, string accessToken, CancellationToken cancellationToken = default) =>
        SendAsync(With(HttpMethod.Post, endpoint.Games(leagueId), game, Json.NewLeagueGame), Json.LeagueView, accessToken, cancellationToken);

    /// <inheritdoc/>
    public Task<LeaguesReply<LeagueView>> RemoveGameAsync(string leagueId, string gameId, string accessToken, CancellationToken cancellationToken = default) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Delete, endpoint.Game(leagueId, gameId)), Json.LeagueView, accessToken, cancellationToken);

    /// <inheritdoc/>
    public Task<LeaguesReply<LeagueView>> SetArmyAsync(string leagueId, LeagueArmyUpdate update, string accessToken, CancellationToken cancellationToken = default) =>
        SendAsync(With(HttpMethod.Put, endpoint.Army(leagueId), update, Json.LeagueArmyUpdate), Json.LeagueView, accessToken, cancellationToken);

    /// <inheritdoc/>
    public Task<LeaguesReply<LeagueView>> StartSeasonAsync(string leagueId, int fromSeason, string accessToken, CancellationToken cancellationToken = default) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Post, endpoint.NextSeason(leagueId, fromSeason)), Json.LeagueView, accessToken, cancellationToken);

    private static HttpRequestMessage With<TBody>(HttpMethod method, Uri address, TBody body, JsonTypeInfo<TBody> type) =>
        new(method, address) { Content = JsonContent.Create(body, type) };

    private async Task<LeaguesReply<TValue>> SendAsync<TValue>(
        HttpRequestMessage request,
        JsonTypeInfo<TValue> type,
        string accessToken,
        CancellationToken cancellationToken)
        where TValue : class
    {
        using (request)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            try
            {
                using var response = await httpClient.SendAsync(request, cancellationToken);
                var status = StatusOf(response.StatusCode);
                return status switch
                {
                    LeaguesStatus.Ok => new LeaguesReply<TValue>(status, JsonSerializer.Deserialize(await HttpBodies.ReadAsync(response, cancellationToken), type), []),
                    LeaguesStatus.Rejected => new LeaguesReply<TValue>(status, null, await ProblemsAsync(response, cancellationToken)),
                    _ => LeaguesReply<TValue>.Of(status)
                };
            }
            catch (Exception ex) when ((ex is HttpRequestException or JsonException or TaskCanceledException) && !cancellationToken.IsCancellationRequested)
            {
                Console.Error.WriteLine($"{request.Method} {request.RequestUri} failed: {ex.Message}");
                return LeaguesReply<TValue>.Of(LeaguesStatus.Unavailable);
            }
        }
    }

    // A rejection lists its problems; one without a readable list (a body too large, say) has none to show.
    private static async Task<IReadOnlyList<string>> ProblemsAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return JsonSerializer.Deserialize(await HttpBodies.ReadAsync(response, cancellationToken), Json.ListString) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static LeaguesStatus StatusOf(HttpStatusCode code) => code switch
    {
        >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous => LeaguesStatus.Ok,
        HttpStatusCode.NotFound => LeaguesStatus.NotFound,
        HttpStatusCode.Unauthorized => LeaguesStatus.Unauthorized,
        HttpStatusCode.Forbidden => LeaguesStatus.Forbidden,
        HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge => LeaguesStatus.Rejected,
        HttpStatusCode.Conflict => LeaguesStatus.Conflict,
        _ => LeaguesStatus.Unavailable
    };
}
