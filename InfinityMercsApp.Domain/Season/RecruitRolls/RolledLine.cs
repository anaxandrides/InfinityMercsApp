using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.Domain.Season.RecruitRolls;

/// <summary>A trooper's line once its recruit table results are on it, with the weapons and equipment they add.</summary>
public sealed record RolledLine(TrooperProfile Profile, IReadOnlyList<string> Weapons, IReadOnlyList<string> Equipment);
