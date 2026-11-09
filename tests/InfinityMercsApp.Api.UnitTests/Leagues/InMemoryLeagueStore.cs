using InfinityMercsApp.Api.Leagues;

namespace InfinityMercsApp.Api.UnitTests.Leagues;

/// <summary>Leagues kept in memory, with an ETag that changes on every write, as Cosmos DB's does.</summary>
internal sealed class InMemoryLeagueStore : ILeagueStore
{
    private readonly Dictionary<string, StoredLeague> _leagues = [];
    private int _version;

    public Dictionary<string, string> Invites { get; } = [];

    /// <summary>How many replacements to refuse, as if someone else wrote first.</summary>
    public int RefuseReplacements { get; set; }

    public LeagueDocument? this[string leagueId] => _leagues.GetValueOrDefault(leagueId)?.League;

    public Task<StoredLeague?> ReadAsync(string leagueId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_leagues.GetValueOrDefault(leagueId));

    public Task<IReadOnlyList<LeagueDocument>> ListForMemberAsync(string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LeagueDocument>>(_leagues.Values.Select(stored => stored.League).Where(league => league.MemberUserIds.Contains(userId)).ToList());

    public Task<bool> TryCreateAsync(LeagueDocument league, CancellationToken cancellationToken = default)
    {
        if (_leagues.ContainsKey(league.LeagueId))
        {
            return Task.FromResult(false);
        }

        Keep(league);
        return Task.FromResult(true);
    }

    public Task<bool> TryReplaceAsync(LeagueDocument league, string etag, CancellationToken cancellationToken = default)
    {
        if (RefuseReplacements > 0)
        {
            RefuseReplacements--;
            return Task.FromResult(false);
        }

        if (_leagues.GetValueOrDefault(league.LeagueId)?.ETag != etag)
        {
            return Task.FromResult(false);
        }

        Keep(league);
        return Task.FromResult(true);
    }

    public Task<string?> FindInviteAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(Invites.GetValueOrDefault(code));

    public Task<bool> TryCreateInviteAsync(LeagueInvite invite, CancellationToken cancellationToken = default) =>
        Task.FromResult(Invites.TryAdd(invite.Code, invite.LeagueId));

    public Task DeleteInviteAsync(string code, CancellationToken cancellationToken = default)
    {
        Invites.Remove(code);
        return Task.CompletedTask;
    }

    private void Keep(LeagueDocument league) => _leagues[league.LeagueId] = new StoredLeague(league, $"etag-{++_version}");
}
