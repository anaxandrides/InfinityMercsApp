namespace InfinityMercsApp.Domain.Models.Leagues;

/// <summary>A member's company season entered in a league, or its renown brought up to date.</summary>
public class LeagueEntryUpdate
{
    /// <summary>The company's renown so far this season, as the member's app works it out.</summary>
    public int Renown { get; set; }
}
