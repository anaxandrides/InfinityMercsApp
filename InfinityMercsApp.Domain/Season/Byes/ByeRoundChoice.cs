namespace InfinityMercsApp.Domain.Season.Byes;

/// <summary>What the player chose for a bye round: how the XP is shared, who gains it, and the injury written off.</summary>
public sealed record ByeRoundChoice(ByeXpSplit Split, IReadOnlyList<ByeTrooper> Troopers, InjuryWriteOff? WriteOff = null);
