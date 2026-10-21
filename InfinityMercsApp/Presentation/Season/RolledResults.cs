using InfinityMercsApp.Domain.Season.RecruitRolls;

namespace InfinityMercsApp.Presentation.Season;

/// <summary>A trooper's Booty and Metachemistry results, with what tells their weapons, skills and equipment apart.</summary>
internal sealed record RolledResults(IReadOnlyList<string> Results, GainKinds Kinds);
