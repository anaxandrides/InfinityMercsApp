using InfinityMercsApp.Domain.Season.Experience;

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
