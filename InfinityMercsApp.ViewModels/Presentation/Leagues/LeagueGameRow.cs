using System.Globalization;
using InfinityMercsApp.Domain.Models.Leagues;

namespace InfinityMercsApp.Presentation.Leagues;

/// <summary>A recorded game as a league's page lists it.</summary>
/// <param name="Score">Each company with its objective points and result, e.g. "Red Company 7 (won) · Blue Company 3".</param>
public sealed record LeagueGameRow(string GameId, string PlayedOn, string Mission, string Score, string RecordedBy, bool CanRemove)
{
    public static LeagueGameRow For(LeagueGameView game, LeagueView league) => new(
        game.GameId,
        game.PlayedOn.ToString("d MMM yyyy", CultureInfo.InvariantCulture),
        game.Mission,
        string.Join(" · ", game.Sides.Select(side => $"{LeagueNames.CompanyOf(league, side.SeasonId)} {side.ObjectivePoints}{ResultNote(side.Result)}")),
        LeagueNames.PlayerOf(league, game.RecordedBy),
        game.CanRemove);

    /// <summary>The games of a league season, latest first.</summary>
    public static IReadOnlyList<LeagueGameRow> ListFor(LeagueView league, int leagueSeason) =>
        league.Games
            .Where(game => game.LeagueSeason == leagueSeason)
            .OrderByDescending(game => game.PlayedOn)
            .Select(game => For(game, league))
            .ToList();

    private static string ResultNote(GameResult result) => result switch
    {
        GameResult.Win => " (won)",
        GameResult.Draw => " (drew)",
        _ => string.Empty
    };
}
