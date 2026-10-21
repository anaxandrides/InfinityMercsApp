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

    /// <summary>
    /// Every active trooper, in company order, for downtime: the deployed ones are the same results as in
    /// <see cref="Units"/>, the rest as they stand.
    /// </summary>
    public List<ExperienceUnitResult> Troopers { get; set; } = [];

    /// <summary>Troopers taken out during the game, who must check for injury.</summary>
    public List<PendingInjury> PendingInjuries { get; set; } = [];
}
