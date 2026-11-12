namespace InfinityMercsApp.Domain.Models.Leagues;

/// <summary>A game between companies in a league, recorded by one of the players.</summary>
public class NewLeagueGame
{
    public DateOnly PlayedOn { get; set; }

    /// <summary>The mission played; may be empty.</summary>
    public string Mission { get; set; } = string.Empty;

    /// <summary>Every company that played, the recording player's among them.</summary>
    public List<LeagueGameSide> Sides { get; set; } = [];
}
