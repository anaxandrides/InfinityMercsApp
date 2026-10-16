namespace InfinityMercsApp.Domain.Season.Renown;

/// <summary>
/// What one trooper adds to its company's renown: its initial cost, the base cost of the gear on its loadout,
/// and its XP.
/// </summary>
public readonly record struct TrooperRenown(int InitialCost, int GearCost, int Experience)
{
    public int Total => Math.Max(0, InitialCost) + Math.Max(0, GearCost) + Math.Max(0, Experience);
}
