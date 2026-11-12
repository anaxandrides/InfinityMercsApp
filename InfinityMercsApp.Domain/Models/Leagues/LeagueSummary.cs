namespace InfinityMercsApp.Domain.Models.Leagues;

/// <summary>A league the user belongs to, as the list of their leagues shows it.</summary>
public class LeagueSummary
{
    public string LeagueId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>The league's season now being played, from 1.</summary>
    public int CurrentSeason { get; set; }

    public int MemberCount { get; set; }

    public bool IsOrganizer { get; set; }
}
