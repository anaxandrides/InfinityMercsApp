namespace InfinityMercsApp.Domain.Season.Game;

/// <summary>
/// A company is Elite Deployed when it deploys the Captain and no more than 3 other troopers (a TAG takes two
/// slots). Its troopers then gain Tactical Awareness, the Captain excepted, and +3 XP.
/// </summary>
public static class EliteDeployment
{
    /// <summary>Whether a trooper has Tactical Awareness this contract: its own, or one an elite deployment gives.</summary>
    public static bool HasTacticalAwareness(bool hasTheSkill, bool isCaptain, bool isEliteDeployment) =>
        hasTheSkill || (isEliteDeployment && !isCaptain);
}
