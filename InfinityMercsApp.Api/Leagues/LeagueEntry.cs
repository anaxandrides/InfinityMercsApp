namespace InfinityMercsApp.Api.Leagues;

/// <summary>A member's company season entered in a league.</summary>
/// <param name="SeasonId">The season's id among its owner's files.</param>
/// <param name="LeagueSeason">The league season it was entered in.</param>
/// <param name="Renown">The company's renown as its owner's app last reported it.</param>
public sealed record LeagueEntry(string SeasonId, string MemberId, string CompanyName, int LeagueSeason, int Renown, DateTimeOffset UpdatedAt);
