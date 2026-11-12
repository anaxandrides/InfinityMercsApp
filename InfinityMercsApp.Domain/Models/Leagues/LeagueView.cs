namespace InfinityMercsApp.Domain.Models.Leagues;

/// <summary>A league as one of its members sees it.</summary>
public class LeagueView
{
    public string LeagueId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>The army data the league plays with; null for whatever is current.</summary>
    public string? ArmySnapshotId { get; set; }

    /// <summary>The league's season now being played, from 1.</summary>
    public int CurrentSeason { get; set; }

    /// <summary>The league season whose entries and games are shown.</summary>
    public int Season { get; set; }

    public bool IsOrganizer { get; set; }

    /// <summary>The code that lets people join; only the organizer sees it, and it is null while joining is closed.</summary>
    public string? InviteCode { get; set; }

    public List<LeagueMemberView> Members { get; set; } = [];

    /// <summary>The company seasons entered in the league season shown.</summary>
    public List<LeagueEntryView> Entries { get; set; } = [];

    /// <summary>The games recorded in the league season shown.</summary>
    public List<LeagueGameView> Games { get; set; } = [];

    /// <summary>The user's company seasons entered in any league season, which can't be entered again.</summary>
    public List<string> EnteredSeasonIds { get; set; } = [];
}
