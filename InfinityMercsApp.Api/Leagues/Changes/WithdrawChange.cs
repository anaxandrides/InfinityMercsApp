namespace InfinityMercsApp.Api.Leagues.Changes;

/// <summary>
/// A company season leaves the league, taken out by its owner or the organizer. One that has played a game stays,
/// since the game's record needs it.
/// </summary>
public sealed class WithdrawChange(string seasonId) : ILeagueChange
{
    /// <inheritdoc/>
    public async Task<LeagueDecision> DecideAsync(LeagueReads reads, string userId, CancellationToken cancellationToken = default)
    {
        if (reads.League.MemberFor(userId) is not { } member ||
            await reads.EntryAsync(seasonId, cancellationToken) is not { Document: var entered } stored)
        {
            return LeagueDecision.NotFound;
        }

        if (entered.MemberId != member.MemberId && !reads.League.IsOrganizer(userId))
        {
            return LeagueDecision.Forbidden;
        }

        // The entry's ETag changes with every game recorded for it, so a game recorded meanwhile stops this.
        return entered.GamesPlayed > 0
            ? LeagueDecision.Rejected($"{entered.CompanyName} has played in the league, so it stays.")
            : LeagueDecision.Done(LeagueWrites.Of(DocumentWrites.Delete(entered, stored.ETag)), entered.LeagueSeason);
    }
}
