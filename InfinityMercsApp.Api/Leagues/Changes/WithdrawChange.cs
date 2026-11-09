namespace InfinityMercsApp.Api.Leagues.Changes;

/// <summary>
/// A company season leaves the league, taken out by its owner or the organizer. One that has played a game stays,
/// since the game's record needs it.
/// </summary>
public sealed class WithdrawChange(string seasonId) : ILeagueChange
{
    /// <inheritdoc/>
    public LeagueChangeResult Apply(LeagueDocument league, string userId)
    {
        if (league.MemberFor(userId) is not { } member ||
            league.Entries.FirstOrDefault(entry => entry.SeasonId == seasonId) is not { } entered)
        {
            return LeagueChangeResult.NotFound;
        }

        if (entered.MemberId != member.MemberId && !league.IsOrganizer(userId))
        {
            return LeagueChangeResult.Forbidden;
        }

        if (league.Games.Any(game => game.Sides.Any(side => side.SeasonId == seasonId)))
        {
            return LeagueChangeResult.Rejected($"{entered.CompanyName} has played in the league, so it stays.");
        }

        return LeagueChangeResult.Done(league with { Entries = league.Entries.Where(entry => entry != entered).ToList() });
    }
}
