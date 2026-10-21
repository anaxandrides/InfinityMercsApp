using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Web.Presentation;

/// <summary>A season as the list of the user's seasons shows it.</summary>
/// <param name="Contracts">Contracts played, byes included.</param>
public sealed record SeasonSummary(string Id, string CompanyName, string Started, int Contracts, int Wins)
{
    public static SeasonSummary From(string id, SeasonFile season) => new(
        id,
        season.CompanyName,
        season.CreatedDate,
        season.Rounds.Count,
        season.Rounds.Count(round => round.MissionResults.Won));
}
