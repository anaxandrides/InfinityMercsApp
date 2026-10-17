namespace InfinityMercsApp.Domain.Models.Season;

public class SeasonTransaction
{
    public string OriginStore { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int CostCr { get; set; }
    public decimal? CostSwc { get; set; }
    public decimal? SwcGrant { get; set; }
    public bool IsSale { get; set; }

    /// <summary>
    /// For armour that rolls on a recruit table when bought, the result this copy rolled; it is named with it in
    /// the inventory.
    /// </summary>
    public string? RolledResult { get; set; }
}
