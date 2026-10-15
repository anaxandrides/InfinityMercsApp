namespace InfinityMercsApp.Domain.Models.Perks;

/// <summary>A trooper rank: the experience needed to reach it, its name, its level (1 for Newbie), and the skill it grants.</summary>
public sealed record ExperienceRank(int MinXp, string Name, int Level, string? FreeSkill = null);
