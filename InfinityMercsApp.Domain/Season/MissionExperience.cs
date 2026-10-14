namespace InfinityMercsApp.Domain.Season;

/// <summary>XP a trooper earned from actions ticked off during the game.</summary>
public sealed class UnitXpData
{
    /// <summary>Two checkboxes, ticked in order.</summary>
    public IList<bool> Assist { get; } = [false, false];
    /// <summary>Three checkboxes, ticked in order.</summary>
    public IList<bool> InflictState { get; } = [false, false, false];
    public bool AttemptButton { get; set; }
    public bool SucceedButton { get; set; }
    public bool ScanEnemy { get; set; }
    public bool ScanEnemyFo { get; set; }
    public bool TagAndBag { get; set; }
    public int DowntimeBonusXp { get; set; }

    public int TotalXp =>
        Assist.Count(b => b) +
        InflictState.Count(b => b) +
        (AttemptButton ? 1 : 0) +
        (SucceedButton ? 1 : 0) +
        (ScanEnemy ? 1 : 0) +
        (ScanEnemyFo ? 1 : 0) +
        (TagAndBag ? MissionExperience.TagAndBagXp : 0) +
        DowntimeBonusXp;
}

/// <summary>A trooper's part in the finished game: how it was deployed, how it ended, and its XP.</summary>
public sealed class ExperienceUnitResult
{
    public int EntryIndex { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CachedLogoPath { get; set; }
    public string? PackagedLogoPath { get; set; }
    public int DeploymentXp { get; set; }
    public bool IsEliteDeployment { get; set; }
    public bool IsConsciousAtEnd { get; set; }
    public bool GainedInjury { get; set; }
    public string? InjuryName { get; set; }
    public UnitXpData XpData { get; init; } = new();
    public bool IsMvp { get; set; }
    public bool IsCaptain { get; set; }
    public bool IsLieutenant { get; set; }
    public string UnitPh { get; set; } = "-";
    public string UnitBs { get; set; } = "-";
    public string UnitCc { get; set; } = "-";
    public string UnitWip { get; set; } = "-";
    public string UnitArm { get; set; } = "-";

    // Cost paid at purchase (includes the loadout) + accumulated XP.
    public int Renown { get; set; }

    // Free-form text — used by downtime to match requirements like "Hacker", "Trinity Program".
    public string Skills { get; set; } = string.Empty;
    public string Equipment { get; set; } = string.Empty;

    public int TotalXp => MissionExperience.Lines(this).Sum(line => line.Xp) + (IsMvp ? MissionExperience.MvpXp : 0);
}

/// <summary>One source of XP shown on the experience screen.</summary>
public sealed record XpLine(string Label, int Xp);

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
