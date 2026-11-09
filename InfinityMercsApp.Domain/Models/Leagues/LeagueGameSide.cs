namespace InfinityMercsApp.Domain.Models.Leagues;

/// <summary>One company's part in a league game.</summary>
public class LeagueGameSide
{
    /// <summary>The company season that played, as it was entered in the league.</summary>
    public string SeasonId { get; set; } = string.Empty;

    public GameResult Result { get; set; }

    public int ObjectivePoints { get; set; }
}
