using InfinityMercsApp.Api.Leagues;

namespace InfinityMercsApp.Api.UnitTests.Leagues;

/// <summary>
/// Leagues and their entries and games kept in memory, each with an ETag that changes on every write, written all or
/// none as Cosmos DB's transactional batches are.
/// </summary>
internal sealed class InMemoryLeagueStore : ILeagueStore
{
    private readonly Dictionary<string, StoredDocument<LeagueDocument>> _leagues = [];
    private readonly Dictionary<string, StoredDocument<LeagueEntry>> _entries = [];
    private readonly Dictionary<string, StoredDocument<LeagueGame>> _games = [];
    private int _version;

    public Dictionary<string, string> Invites { get; } = [];

    /// <summary>How many commits to refuse, as if someone else wrote first.</summary>
    public int RefuseCommits { get; set; }

    public LeagueDocument? this[string leagueId] => _leagues.GetValueOrDefault(leagueId)?.Document;

    public LeagueEntry? Entry(string leagueId, string seasonId) => _entries.GetValueOrDefault(Key(leagueId, LeagueEntry.IdFor(seasonId)))?.Document;

    public LeagueGame? Game(string leagueId, string gameId) => _games.GetValueOrDefault(Key(leagueId, LeagueGame.IdFor(gameId)))?.Document;

    public int GameCount => _games.Count;

    public void Keep(LeagueDocument league) => _leagues[league.LeagueId] = Stored(league);

    public void Keep(LeagueEntry entry) => _entries[Key(entry)] = Stored(entry);

    public void Keep(LeagueGame game) => _games[Key(game)] = Stored(game);

    public Task<StoredDocument<LeagueDocument>?> ReadAsync(string leagueId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_leagues.GetValueOrDefault(leagueId));

    public Task<IReadOnlyList<LeagueDocument>> ListForMemberAsync(string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LeagueDocument>>(_leagues.Values.Select(stored => stored.Document).Where(league => league.MemberUserIds.Contains(userId)).ToList());

    public Task<bool> TryCreateAsync(LeagueDocument league, CancellationToken cancellationToken = default)
    {
        if (_leagues.ContainsKey(league.LeagueId))
        {
            return Task.FromResult(false);
        }

        Keep(league);
        return Task.FromResult(true);
    }

    public Task<StoredDocument<LeagueEntry>?> ReadEntryAsync(string leagueId, string seasonId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_entries.GetValueOrDefault(Key(leagueId, LeagueEntry.IdFor(seasonId))));

    public Task<StoredDocument<LeagueGame>?> ReadGameAsync(string leagueId, string gameId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_games.GetValueOrDefault(Key(leagueId, LeagueGame.IdFor(gameId))));

    public Task<IReadOnlyList<LeagueEntry>> ListEntriesAsync(string leagueId, int leagueSeason, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LeagueEntry>>(_entries.Values.Select(stored => stored.Document)
            .Where(entry => entry.LeagueId == leagueId && entry.LeagueSeason == leagueSeason).ToList());

    public Task<IReadOnlyList<LeagueGame>> ListGamesAsync(string leagueId, int leagueSeason, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LeagueGame>>(_games.Values.Select(stored => stored.Document)
            .Where(game => game.LeagueId == leagueId && game.LeagueSeason == leagueSeason).ToList());

    public Task<IReadOnlyList<string>> ListEnteredSeasonIdsAsync(string leagueId, string memberId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(_entries.Values.Select(stored => stored.Document)
            .Where(entry => entry.LeagueId == leagueId && entry.MemberId == memberId).Select(entry => entry.SeasonId).ToList());

    public Task<bool> TryCommitAsync(string leagueId, LeagueWrites writes, CancellationToken cancellationToken = default)
    {
        if (RefuseCommits > 0)
        {
            RefuseCommits--;
            return Task.FromResult(false);
        }

        var inPartition = (writes.League is null || writes.League.Document.LeagueId == leagueId) &&
                          writes.Entries.All(write => write.Document.LeagueId == leagueId) &&
                          writes.Games.All(write => write.Document.LeagueId == leagueId);
        var allowed = (writes.League is null || Allows(_leagues, leagueId, writes.League)) &&
                      writes.Entries.All(write => Allows(_entries, Key(write.Document), write)) &&
                      writes.Games.All(write => Allows(_games, Key(write.Document), write));
        if (!inPartition || !allowed)
        {
            return Task.FromResult(false);
        }

        if (writes.League is { } league)
        {
            Apply(_leagues, leagueId, league);
        }

        foreach (var entry in writes.Entries)
        {
            Apply(_entries, Key(entry.Document), entry);
        }

        foreach (var game in writes.Games)
        {
            Apply(_games, Key(game.Document), game);
        }

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

    private static bool Allows<T>(Dictionary<string, StoredDocument<T>> kept, string key, DocumentWrite<T> write)
        where T : ILeagueItem =>
        write.Kind == DocumentWriteKind.Create
            ? !kept.ContainsKey(key)
            : kept.GetValueOrDefault(key)?.ETag is { } etag && etag == write.ETag;

    private void Apply<T>(Dictionary<string, StoredDocument<T>> kept, string key, DocumentWrite<T> write)
        where T : ILeagueItem
    {
        if (write.Kind == DocumentWriteKind.Delete)
        {
            kept.Remove(key);
        }
        else
        {
            kept[key] = Stored(write.Document);
        }
    }

    private StoredDocument<T> Stored<T>(T document) => new(document, $"etag-{++_version}");

    private static string Key(ILeagueItem item) => Key(item.LeagueId, item.Id);

    private static string Key(string leagueId, string id) => $"{leagueId}/{id}";
}
