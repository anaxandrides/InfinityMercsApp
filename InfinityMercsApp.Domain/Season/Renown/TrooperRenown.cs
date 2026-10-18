namespace InfinityMercsApp.Domain.Season.Renown;

/// <summary>
/// What one trooper adds to its company's renown: its initial cost, the base cost of the gear on its loadout,
/// its XP, and for a Lone Wolf 10 a rank.
/// </summary>
public readonly record struct TrooperRenown(int InitialCost, int GearCost, int Experience, int RankRenown = 0)
{
    public int Total => WithoutGear + Math.Max(0, GearCost);

    /// <summary>What a retiring trooper keeps: its gear goes back to the inventory, so only the rest counts.</summary>
    public int WithoutGear => Math.Max(0, InitialCost) + Math.Max(0, Experience) + Math.Max(0, RankRenown);
}
