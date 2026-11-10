namespace InfinityMercsApp.Api.Leagues.Changes;

/// <summary>
/// A member enters one of their company seasons in the league's current season, or brings the renown of one they
/// entered up to date.
/// </summary>
public sealed class EnterSeasonChange(string seasonId, string companyName, int renown, DateTimeOffset now) : ILeagueChange
{
    /// <inheritdoc/>
    public async Task<LeagueDecision> DecideAsync(LeagueReads reads, string userId, CancellationToken cancellationToken = default)
    {
        var league = reads.League;
        if (league.MemberFor(userId) is not { } member)
        {
            return LeagueDecision.NotFound;
        }

        if (await reads.EntryAsync(seasonId, cancellationToken) is { Document: var entered } stored)
        {
            if (entered.MemberId != member.MemberId)
            {
                return LeagueDecision.Forbidden;
            }

            var updated = entered with { CompanyName = companyName, Renown = renown, UpdatedAt = now };
            return LeagueDecision.Done(LeagueWrites.Of(DocumentWrites.Replace(updated, stored.ETag)), entered.LeagueSeason);
        }

        var entry = LeagueEntry.For(league.LeagueId, seasonId, member.MemberId, companyName, league.CurrentSeason, renown, now);
        return LeagueDecision.Done(LeagueWrites.Of(DocumentWrites.Create(entry)), league.CurrentSeason);
    }
}
