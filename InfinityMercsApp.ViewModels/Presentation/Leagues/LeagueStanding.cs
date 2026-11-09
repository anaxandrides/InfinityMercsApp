namespace InfinityMercsApp.Presentation.Leagues;

/// <summary>One company's line in a league season's standings.</summary>
/// <param name="Renown">The company's renown as its owner's app last reported it.</param>
/// <param name="IsYours">Whether the company belongs to the user looking.</param>
public sealed record LeagueStanding(
    int Place,
    string SeasonId,
    string CompanyName,
    string PlayerName,
    int Renown,
    int Played,
    int Won,
    int Drawn,
    int Lost,
    int ObjectivePoints,
    bool IsYours)
{
    /// <summary>Wins, draws and losses, e.g. "3-1-0".</summary>
    public string Record => $"{Won}-{Drawn}-{Lost}";
}
