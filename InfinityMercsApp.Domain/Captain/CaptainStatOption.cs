using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.Domain.Captain;

/// <summary>One choosable upgrade level of a captain stat: its total bonus and total experience cost.</summary>
public sealed record CaptainStatOption(Stat Stat, int Tier, int Bonus, int Cost);
