namespace InfinityMercsApp.Domain.Models.Perks;

/// <summary>A dependency within a track, from the tier that unlocks to the tier unlocked.</summary>
public readonly record struct PerkTableLink(int Track, int FromTier, int ToTier);
