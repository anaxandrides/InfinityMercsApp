using InfinityMercsApp.Domain.Models.Leagues;

namespace InfinityMercsApp.Api.Leagues;

/// <summary>A game recorded in a league, kept as its own document in the league's partition.</summary>
/// <param name="Id">The document id, made from the game's id.</param>
/// <param name="RecordedBy">The member who recorded it.</param>
public sealed record LeagueGame(
    string Id,
    string LeagueId,
    string GameId,
    int LeagueSeason,
    DateOnly PlayedOn,
    string Mission,
    string RecordedBy,
    DateTimeOffset RecordedAt,
    IReadOnlyList<LeagueGameSide> Sides) : ILeagueItem
{
    public string Kind => LeagueItemKinds.Game;

    public static string IdFor(string gameId) => $"game-{gameId}";
}
