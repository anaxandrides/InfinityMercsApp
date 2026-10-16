using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.Domain.Captain;

/// <summary>The upgrade tier picked for one of a captain's stats, and the stat's value before it.</summary>
public sealed record CaptainStatPick(Stat Stat, int BaseValue, int Tier)
{
    /// <summary>The picked tier as the rules offer it; null when the rules offer no such tier.</summary>
    public CaptainStatOption? Option => CaptainUpgradeRules.StatOptions(Stat, BaseValue).FirstOrDefault(option => option.Tier == Tier);
}
