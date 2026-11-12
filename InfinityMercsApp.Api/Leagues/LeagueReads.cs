namespace InfinityMercsApp.Api.Leagues;

/// <summary>
/// What a change reads of a league: the league as read, and the entries and games it asks for, each with the ETag a
/// write to it must match.
/// </summary>
public sealed class LeagueReads(ILeagueStore store, StoredDocument<LeagueDocument> league)
{
    public LeagueDocument League => league.Document;

    /// <summary>The ETag the league was read at.</summary>
    public string ETag => league.ETag;

    /// <summary>The entry for a season, or null when it isn't entered.</summary>
    public Task<StoredDocument<LeagueEntry>?> EntryAsync(string seasonId, CancellationToken cancellationToken = default) =>
        store.ReadEntryAsync(League.LeagueId, seasonId, cancellationToken);

    /// <summary>A recorded game, or null when there is none by that id.</summary>
    public Task<StoredDocument<LeagueGame>?> GameAsync(string gameId, CancellationToken cancellationToken = default) =>
        store.ReadGameAsync(League.LeagueId, gameId, cancellationToken);
}
