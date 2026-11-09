namespace InfinityMercsApp.Api.Leagues;

/// <summary>Where leagues and their invite codes are kept.</summary>
public interface ILeagueStore
{
    /// <summary>The league, or null when there is none with that id.</summary>
    Task<StoredLeague?> ReadAsync(string leagueId, CancellationToken cancellationToken = default);

    /// <summary>The leagues <paramref name="userId"/> belongs to.</summary>
    Task<IReadOnlyList<LeagueDocument>> ListForMemberAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Keeps a new league; false when one with its id already exists.</summary>
    Task<bool> TryCreateAsync(LeagueDocument league, CancellationToken cancellationToken = default);

    /// <summary>Replaces a league; false when it changed since it was read as <paramref name="etag"/>.</summary>
    Task<bool> TryReplaceAsync(LeagueDocument league, string etag, CancellationToken cancellationToken = default);

    /// <summary>The league an invite code leads to, or null when no league has that code.</summary>
    Task<string?> FindInviteAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Keeps an invite code; false when the code is already taken.</summary>
    Task<bool> TryCreateInviteAsync(LeagueInvite invite, CancellationToken cancellationToken = default);

    /// <summary>Forgets an invite code, if it is kept.</summary>
    Task DeleteInviteAsync(string code, CancellationToken cancellationToken = default);
}
