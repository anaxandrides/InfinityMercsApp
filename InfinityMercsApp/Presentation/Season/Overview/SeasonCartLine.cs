namespace InfinityMercsApp.Presentation.Season.Overview;

/// <summary>Some of one item waiting to be bought, with the price and category it will be recorded at.</summary>
public sealed record SeasonCartLine(string StoreName, string ItemName, string Category, int CostCr, decimal? CostSwc, int Quantity)
{
    public int TotalCr => CostCr * Quantity;
}
