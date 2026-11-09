using InfinityMercsApp.Domain.Models.Leagues;

namespace InfinityMercsApp.Api.Leagues;

/// <summary>A game recorded in a league.</summary>
/// <param name="RecordedBy">The member who recorded it.</param>
public sealed record LeagueGame(
    string GameId,
    int LeagueSeason,
    DateOnly PlayedOn,
    string Mission,
    string RecordedBy,
    DateTimeOffset RecordedAt,
    IReadOnlyList<LeagueGameSide> Sides);
