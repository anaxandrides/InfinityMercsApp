namespace InfinityMercsApp.Api.Leagues.Changes;

/// <summary>
/// A game is taken off the record, by the member who recorded it or by the organizer; its companies' entries stop
/// counting it.
/// </summary>
public sealed class RemoveGameChange(string gameId) : ILeagueChange
{
    /// <inheritdoc/>
    public async Task<LeagueDecision> DecideAsync(LeagueReads reads, string userId, CancellationToken cancellationToken = default)
    {
        if (reads.League.MemberFor(userId) is not { } member ||
            await reads.GameAsync(gameId, cancellationToken) is not { Document: var recorded } stored)
        {
            return LeagueDecision.NotFound;
        }

        if (recorded.RecordedBy != member.MemberId && !reads.League.IsOrganizer(userId))
        {
            return LeagueDecision.Forbidden;
        }

        var entries = new List<DocumentWrite<LeagueEntry>>();
        foreach (var side in recorded.Sides)
        {
            if (await reads.EntryAsync(side.SeasonId, cancellationToken) is { Document: var entry } entered)
            {
                entries.Add(DocumentWrites.Replace(entry with { GamesPlayed = Math.Max(0, entry.GamesPlayed - 1) }, entered.ETag));
            }
        }

        return LeagueDecision.Done(new LeagueWrites(null, entries, [DocumentWrites.Delete(recorded, stored.ETag)]), recorded.LeagueSeason);
    }
}
