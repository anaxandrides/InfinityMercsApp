namespace InfinityMercsApp.Domain.Season;

/// <summary>The XP a trooper earns from a mission.</summary>
public static class MissionExperience
{
    public const int EliteDeploymentXp = 3;
    public const int SurvivedXp = 2;
    public const int InjuryXp = 2;
    public const int MvpXp = 2;
    public const int TagAndBagXp = 2;

    /// <summary>
    /// Every XP source the trooper earned, in display order, excluding MVP (which is chosen afterwards).
    /// Deployment XP equals the campaign mission number.
    /// </summary>
    public static IReadOnlyList<XpLine> Lines(ExperienceUnitResult unit)
    {
        var lines = new List<XpLine> { new($"Deployed (Mission {unit.DeploymentXp})", unit.DeploymentXp) };

        if (unit.IsEliteDeployment)
            lines.Add(new("Elite Deployment", EliteDeploymentXp));
        if (unit.IsConsciousAtEnd)
            lines.Add(new("Survived", SurvivedXp));
        if (unit.GainedInjury)
            lines.Add(new("Injury Sustained", InjuryXp));

        var xp = unit.XpData;
        var assists = xp.Assist.Count(b => b);
        if (assists > 0)
            lines.Add(new(assists == 1 ? "Assisted" : $"Assisted ×{assists}", assists));
        var states = xp.InflictState.Count(b => b);
        if (states > 0)
            lines.Add(new(states == 1 ? "Inflicted State" : $"Inflicted State ×{states}", states));
        if (xp.AttemptButton)
            lines.Add(new("Attempted Objective", 1));
        if (xp.SucceedButton)
            lines.Add(new("Completed Objective", 1));
        if (xp.ScanEnemy)
            lines.Add(new("Scan Enemy", 1));
        if (xp.ScanEnemyFo)
            lines.Add(new("Scan Enemy (FO)", 1));
        if (xp.TagAndBag)
            lines.Add(new("Tag and Bag", TagAndBagXp));
        if (xp.DowntimeBonusXp != 0)
            lines.Add(new("Downtime Bonus", xp.DowntimeBonusXp));

        return lines;
    }
}
