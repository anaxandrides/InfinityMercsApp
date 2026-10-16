using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.Domain.Captain;

/// <summary>The upgrade tier picked for one of a captain's stats, the stat's value before it, and the chart it comes from.</summary>
public sealed record CaptainStatPick(Stat Stat, int BaseValue, int Tier, CaptainStatChart Chart)
{
    /// <summary>The picked tier as the rules offer it; null when the rules offer no such tier.</summary>
    public CaptainStatOption? Option => CaptainUpgradeRules.StatOptions(Stat, BaseValue, Chart).FirstOrDefault(option => option.Tier == Tier);
}
