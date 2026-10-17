namespace InfinityMercsApp.Domain.Season.RecruitRolls;

/// <summary>A roll a trooper has yet to make, and whether its skill (Booty (Reroll)) lets it roll a second time.</summary>
public sealed record OwedRoll(RecruitRollTable Table, bool CanReroll);
