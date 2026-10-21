namespace InfinityMercsApp.Domain.Season.Downtime;

/// <summary>
/// How an event involves the opponent of the last contract: with Opponent they gain the pass benefits when you fail;
/// with Opponent (Mutual) they gain what you gain when you pass. Either way, never Notoriety.
/// </summary>
public enum OpponentKind { None, Opponent, Mutual }
