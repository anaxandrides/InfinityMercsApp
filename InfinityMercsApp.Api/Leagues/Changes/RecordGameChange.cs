using InfinityMercsApp.Domain.Leagues;
using InfinityMercsApp.Domain.Models.Leagues;

namespace InfinityMercsApp.Api.Leagues.Changes;

/// <summary>
/// A member records a game their company played against other companies in the league's current season. One player
/// records it for everyone.
/// </summary>
public sealed class RecordGameChange(NewLeagueGame game, string gameId, DateTimeOffset now) : ILeagueChange
{
    /// <inheritdoc/>
    public LeagueChangeResult Apply(LeagueDocument league, string userId)
    {
        if (league.MemberFor(userId) is not { } member)
        {
            return LeagueChangeResult.NotFound;
        }

        var entries = new List<LeagueEntry>();
        foreach (var side in game.Sides)
        {
            if (league.Entries.FirstOrDefault(entry => entry.SeasonId == side.SeasonId) is not { } entry)
            {
                return LeagueChangeResult.Rejected("A company in the game isn't in the league.");
            }

            if (entry.LeagueSeason != league.CurrentSeason)
            {
                return LeagueChangeResult.Rejected($"{entry.CompanyName} was in an earlier league season.");
            }

            entries.Add(entry);
        }

        if (entries.All(entry => entry.MemberId != member.MemberId))
        {
            return LeagueChangeResult.Rejected("Only a player whose company played can record the game.");
        }

        if (league.Games.Count >= LeagueLimits.Games)
        {
            return LeagueChangeResult.Rejected($"The league can't record more than {LeagueLimits.Games} games.");
        }

        var sides = game.Sides
            .Select(side => new LeagueGameSide { SeasonId = side.SeasonId, Result = side.Result, ObjectivePoints = side.ObjectivePoints })
            .ToList();
        var recorded = new LeagueGame(gameId, league.CurrentSeason, game.PlayedOn, game.Mission.Trim(), member.MemberId, now, sides);
        return LeagueChangeResult.Done(league with { Games = [.. league.Games, recorded] });
    }
}
