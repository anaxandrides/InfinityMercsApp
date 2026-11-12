namespace InfinityMercsApp.Api.Leagues;

/// <summary>A member's company season entered in a league, kept as its own document in the league's partition.</summary>
/// <param name="Id">The document id, made from the season's id.</param>
/// <param name="SeasonId">The season's id among its owner's files.</param>
/// <param name="LeagueSeason">The league season it was entered in.</param>
/// <param name="Renown">The company's renown as its owner's app last reported it.</param>
/// <param name="GamesPlayed">How many recorded games it played; one that has played can't be withdrawn.</param>
public sealed record LeagueEntry(
    string Id,
    string LeagueId,
    string SeasonId,
    string MemberId,
    string CompanyName,
    int LeagueSeason,
    int Renown,
    int GamesPlayed,
    DateTimeOffset UpdatedAt) : ILeagueItem
{
    public string Kind => LeagueItemKinds.Entry;

    public static string IdFor(string seasonId) => $"entry-{seasonId}";

    /// <summary>A season newly entered, yet to play.</summary>
    public static LeagueEntry For(string leagueId, string seasonId, string memberId, string companyName, int leagueSeason, int renown, DateTimeOffset now) =>
        new(IdFor(seasonId), leagueId, seasonId, memberId, companyName, leagueSeason, renown, GamesPlayed: 0, now);
}
