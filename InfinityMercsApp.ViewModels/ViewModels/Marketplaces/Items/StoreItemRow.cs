using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Models.Stores;

namespace InfinityMercsApp.ViewModels.Marketplaces.Items;

/// <summary>A store item, at its own price or at <paramref name="costCr"/> when the price is adjusted.</summary>
public sealed class StoreItemRow(StoreItem item, int? costCr = null) : MarketplaceRow(item.Name, costCr ?? item.CostCr)
{
    public string Name => item.Name;
    public override decimal? CostSwc => item.CostSwc;
    public string Category => item.Category;
    public override string InventoryCategory => InventoryCategories.Normalize(item.Category);
    public string? WikiUrl => item.WikiUrl;
    public string? WikiSection => item.WikiSection;

    public override string CostDisplay => SeasonEconomy.PriceText(CostCr, CostSwc);
}
