namespace InfinityMercsApp.Api.Leagues;

/// <summary>Where leagues, their entries and games, and their invite codes are kept.</summary>
public interface ILeagueStore
{
    /// <summary>The league, or null when there is none with that id.</summary>
    Task<StoredDocument<LeagueDocument>?> ReadAsync(string leagueId, CancellationToken cancellationToken = default);

    /// <summary>The leagues <paramref name="userId"/> belongs to.</summary>
    Task<IReadOnlyList<LeagueDocument>> ListForMemberAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Keeps a new league; false when one with its id already exists.</summary>
    Task<bool> TryCreateAsync(LeagueDocument league, CancellationToken cancellationToken = default);

    /// <summary>The entry for a season, or null when it isn't entered.</summary>
    Task<StoredDocument<LeagueEntry>?> ReadEntryAsync(string leagueId, string seasonId, CancellationToken cancellationToken = default);

    /// <summary>A recorded game, or null when there is none by that id.</summary>
    Task<StoredDocument<LeagueGame>?> ReadGameAsync(string leagueId, string gameId, CancellationToken cancellationToken = default);

    /// <summary>The company seasons entered in one league season.</summary>
    Task<IReadOnlyList<LeagueEntry>> ListEntriesAsync(string leagueId, int leagueSeason, CancellationToken cancellationToken = default);

    /// <summary>The games recorded in one league season.</summary>
    Task<IReadOnlyList<LeagueGame>> ListGamesAsync(string leagueId, int leagueSeason, CancellationToken cancellationToken = default);

    /// <summary>The ids of the company seasons a member has entered, in any league season.</summary>
    Task<IReadOnlyList<string>> ListEnteredSeasonIdsAsync(string leagueId, string memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes every write in the league's partition, or none: false when a document changed since it was read, or one
    /// being created already exists.
    /// </summary>
    Task<bool> TryCommitAsync(string leagueId, LeagueWrites writes, CancellationToken cancellationToken = default);

    /// <summary>The league an invite code leads to, or null when no league has that code.</summary>
    Task<string?> FindInviteAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Keeps an invite code; false when the code is already taken.</summary>
    Task<bool> TryCreateInviteAsync(LeagueInvite invite, CancellationToken cancellationToken = default);

    /// <summary>Forgets an invite code, if it is kept.</summary>
    Task DeleteInviteAsync(string code, CancellationToken cancellationToken = default);
}
