namespace InfinityMercsApp.Api.Leagues.Changes;

/// <summary>A game is taken off the record, by the member who recorded it or by the organizer.</summary>
public sealed class RemoveGameChange(string gameId) : ILeagueChange
{
    /// <inheritdoc/>
    public LeagueChangeResult Apply(LeagueDocument league, string userId)
    {
        if (league.MemberFor(userId) is not { } member ||
            league.Games.FirstOrDefault(game => game.GameId == gameId) is not { } recorded)
        {
            return LeagueChangeResult.NotFound;
        }

        return recorded.RecordedBy == member.MemberId || league.IsOrganizer(userId)
            ? LeagueChangeResult.Done(league with { Games = league.Games.Where(game => game != recorded).ToList() })
            : LeagueChangeResult.Forbidden;
    }
}
