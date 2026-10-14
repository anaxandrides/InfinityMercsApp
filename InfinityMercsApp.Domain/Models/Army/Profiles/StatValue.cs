namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>One stat as shown on a statline, e.g. MOV "10-5" or CC "14".</summary>
public sealed record StatValue(Stat Stat, string Value);
