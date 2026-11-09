namespace InfinityMercsApp.Domain.Models.Leagues;

/// <summary>A game recorded in a league.</summary>
public class LeagueGameView
{
    public string GameId { get; set; } = string.Empty;

    /// <summary>The league season it was played in.</summary>
    public int LeagueSeason { get; set; }

    public DateOnly PlayedOn { get; set; }

    public string Mission { get; set; } = string.Empty;

    /// <summary>The member who recorded it.</summary>
    public string RecordedBy { get; set; } = string.Empty;

    public List<LeagueGameSide> Sides { get; set; } = [];

    /// <summary>Whether the user asking may remove it: they recorded it, or they organize the league.</summary>
    public bool CanRemove { get; set; }
}
