namespace InfinityMercsApp.Domain.Models.Army.SpecOps;

/// <summary>What one part of a Spec-Ops or Team-Ops upgrade grants.</summary>
public enum SpecUpgradePartKind
{
    /// <summary>A statline change, e.g. +3 BTS or MOV 15-10.</summary>
    Stat,
    Skill,
    Equipment,
    Weapon
}
