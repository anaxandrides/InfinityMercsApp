using InfinityMercsApp.Domain.Models.Leagues;

namespace InfinityMercsApp.Presentation.Leagues;

/// <summary>A league season's standings: the companies entered in it, by renown, then wins, then objective points.</summary>
public static class LeagueStandings
{
    public static IReadOnlyList<LeagueStanding> For(LeagueView league, int leagueSeason)
    {
        var games = league.Games.Where(game => game.LeagueSeason == leagueSeason).ToList();
        var lines = league.Entries
            .Where(entry => entry.LeagueSeason == leagueSeason)
            .Select(entry =>
            {
                var sides = games.SelectMany(game => game.Sides).Where(side => side.SeasonId == entry.SeasonId).ToList();
                return new LeagueStanding(
                    0,
                    entry.SeasonId,
                    entry.CompanyName,
                    LeagueNames.PlayerOf(league, entry.MemberId),
                    entry.Renown,
                    sides.Count,
                    sides.Count(side => side.Result == GameResult.Win),
                    sides.Count(side => side.Result == GameResult.Draw),
                    sides.Count(side => side.Result == GameResult.Loss),
                    sides.Sum(side => side.ObjectivePoints),
                    entry.IsYours);
            })
            .OrderByDescending(line => line.Renown)
            .ThenByDescending(line => line.Won)
            .ThenByDescending(line => line.ObjectivePoints)
            .ThenBy(line => line.CompanyName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        return lines.Select((line, index) => line with { Place = index + 1 }).ToList();
    }
}
