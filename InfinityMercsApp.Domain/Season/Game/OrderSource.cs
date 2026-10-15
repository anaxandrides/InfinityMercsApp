namespace InfinityMercsApp.Domain.Season.Game;

/// <summary>What about a trooper decides the orders it gives.</summary>
public sealed record OrderSource(bool IsLieutenant, bool IsImpetuous, bool IsIrregular, bool IsConscious);
