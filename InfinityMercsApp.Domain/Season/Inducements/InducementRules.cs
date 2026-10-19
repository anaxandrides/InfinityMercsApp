namespace InfinityMercsApp.Domain.Season.Inducements;

/// <summary>
/// Inducements: the underdog of a contract gets half the difference in deployed renown, rounded down to a multiple
/// of 5, to spend on that contract only. A cooperative contract gives none.
/// </summary>
public static class InducementRules
{
    /// <summary>The inducements one command token costs.</summary>
    public const int CommandTokenCost = 10;

    /// <summary>The most command tokens a company can buy for a contract.</summary>
    public const int MaxCommandTokens = 2;

    /// <summary>The troopers a company deploys without hired troops.</summary>
    public const int TrooperLimit = 6;

    /// <summary>The troopers a company may deploy once it hires troops.</summary>
    public const int TrooperLimitWithHired = 7;

    /// <summary>The inducements a company with <paramref name="ownRenown"/> gets against <paramref name="opponentRenown"/>.</summary>
    public static int For(int ownRenown, int opponentRenown, bool isCooperative) =>
        isCooperative ? 0 : Of(opponentRenown - ownRenown);

    /// <summary>Half of a renown difference, rounded down to a multiple of 5; nothing when it isn't positive.</summary>
    public static int Of(int renownDifference) => renownDifference <= 0 ? 0 : renownDifference / 10 * 5;

    /// <summary>
    /// How many of the hired troops give no orders but impetuous ones: those beyond the six-trooper limit. Hired troops
    /// deploy after the company's own, so the last ones are over.
    /// </summary>
    public static int HiresBeyondLimit(int companySlots, int hiredCount) =>
        Math.Clamp(companySlots + hiredCount - TrooperLimit, 0, hiredCount);
}
