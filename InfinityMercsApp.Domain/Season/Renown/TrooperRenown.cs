namespace InfinityMercsApp.Domain.Season.Renown;

/// <summary>
/// What one trooper adds to its company's renown: its initial cost, the base cost of the gear on its loadout,
/// and its XP.
/// </summary>
public readonly record struct TrooperRenown(int InitialCost, int GearCost, int Experience)
{
    public int Total => Math.Max(0, InitialCost) + Math.Max(0, GearCost) + Math.Max(0, Experience);

    /// <summary>What a retiring trooper keeps: its gear goes back to the inventory, so only its cost and XP count.</summary>
    public int WithoutGear => Math.Max(0, InitialCost) + Math.Max(0, Experience);
}
