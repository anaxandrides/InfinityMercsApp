namespace InfinityMercsApp.Domain.Season;

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
