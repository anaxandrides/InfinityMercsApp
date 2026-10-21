namespace InfinityMercsApp.Domain.Season.Game;

/// <summary>What about a trooper decides the orders it gives.</summary>
/// <param name="IsOverTrooperLimit">A hired troop beyond the six-trooper limit: it gives an impetuous order, if it has one, but no other.</param>
public sealed record OrderSource(bool IsLieutenant, bool IsImpetuous, bool IsIrregular, bool IsConscious, bool IsOverTrooperLimit = false);
