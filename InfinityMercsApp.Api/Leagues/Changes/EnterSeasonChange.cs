using InfinityMercsApp.Domain.Leagues;

namespace InfinityMercsApp.Api.Leagues.Changes;

/// <summary>
/// A member enters one of their company seasons in the league's current season, or brings the renown of one they
/// entered up to date.
/// </summary>
public sealed class EnterSeasonChange(string seasonId, string companyName, int renown, DateTimeOffset now) : ILeagueChange
{
    /// <inheritdoc/>
    public LeagueChangeResult Apply(LeagueDocument league, string userId)
    {
        if (league.MemberFor(userId) is not { } member)
        {
            return LeagueChangeResult.NotFound;
        }

        var entered = league.Entries.FirstOrDefault(entry => entry.SeasonId == seasonId);
        if (entered is not null)
        {
            if (entered.MemberId != member.MemberId)
            {
                return LeagueChangeResult.Forbidden;
            }

            var updated = entered with { CompanyName = companyName, Renown = renown, UpdatedAt = now };
            return LeagueChangeResult.Done(league with { Entries = league.Entries.Select(entry => entry == entered ? updated : entry).ToList() });
        }

        if (league.Entries.Count >= LeagueLimits.Entries)
        {
            return LeagueChangeResult.Rejected($"The league can't take more than {LeagueLimits.Entries} companies.");
        }

        var entry = new LeagueEntry(seasonId, member.MemberId, companyName, league.CurrentSeason, renown, now);
        return LeagueChangeResult.Done(league with { Entries = [.. league.Entries, entry] });
    }
}
