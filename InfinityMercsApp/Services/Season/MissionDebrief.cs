namespace InfinityMercsApp.Services.Season;

/// <summary>
/// The result of the game just played, shared by the post-game screens (injuries, mission outcome,
/// experience, downtime) until the next game replaces it.
/// </summary>
public sealed class MissionDebrief
{
    /// <summary>The deployed troopers and the XP each earned.</summary>
    public List<ExperienceUnitResult> Units { get; set; } = [];

    /// <summary>Troopers taken out during the game, who must check for injury.</summary>
    public List<PendingInjury> PendingInjuries { get; set; } = [];
}

/// <summary>A trooper taken out during the game.</summary>
public sealed class PendingInjury
{
    public int EntryIndex { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PhValue { get; set; } = "-";
    public string? CachedLogoPath { get; set; }
    public string? PackagedLogoPath { get; set; }
}
