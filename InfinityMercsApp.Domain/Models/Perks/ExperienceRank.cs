namespace InfinityMercsApp.Domain.Models.Perks;

/// <summary>A trooper rank: the experience needed to reach it, its name, and its level (1 for Newbie).</summary>
public sealed record ExperienceRank(int MinXp, string Name, int Level);
