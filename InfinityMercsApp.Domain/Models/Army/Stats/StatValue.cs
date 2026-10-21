namespace InfinityMercsApp.Domain.Models.Army.Stats;

/// <summary>One stat as shown on a statline, e.g. MOV "10-5" or CC "14".</summary>
public sealed record StatValue(Stat Stat, string Value)
{
    /// <summary>The stat's heading, e.g. "MOV" or "VITA".</summary>
    public string Key => Stat.Key();
}
