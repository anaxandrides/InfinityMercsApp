using InfinityMercsApp.Domain.Leagues;

namespace InfinityMercsApp.Api.Leagues.Changes;

/// <summary>A user joins the league; joining again changes nothing.</summary>
public sealed class JoinChange(string displayName, string memberId, DateTimeOffset now) : ILeagueChange
{
    /// <inheritdoc/>
    public LeagueChangeResult Apply(LeagueDocument league, string userId)
    {
        if (league.MemberFor(userId) is not null)
        {
            return LeagueChangeResult.Done(league);
        }

        if (league.Members.Count >= LeagueLimits.Members)
        {
            return LeagueChangeResult.Rejected($"The league is full: it has {LeagueLimits.Members} members.");
        }

        return LeagueChangeResult.Done(league.WithMembers([.. league.Members, new LeagueMember(memberId, userId, displayName.Trim(), now)]));
    }
}
