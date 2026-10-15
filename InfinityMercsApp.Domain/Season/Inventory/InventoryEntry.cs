namespace InfinityMercsApp.Domain.Season.Inventory;

/// <summary>
/// Gear the company owns: how many, where it came from and which rounds it was gained in (0 for the
/// company's starting purchases).
/// </summary>
public sealed record InventoryEntry(
    string Name,
    string Category,
    int CostCr,
    int Count,
    IReadOnlyList<string> Sources,
    IReadOnlyList<int> Rounds)
{
    /// <summary>What it sells for: half its price, rounded up; 0 when it has no price and cannot be sold.</summary>
    public int SellPrice => CostCr > 0 ? (int)Math.Ceiling(CostCr / 2.0) : 0;
}
