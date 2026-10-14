using InfinityMercsApp.Domain.Models.Stores;

namespace InfinityMercsApp.ViewModels.Marketplaces;

public sealed class StoreItemRow(StoreItem item) : MarketplaceRow(item.Name, item.CostCr)
{
    public string Name => item.Name;
    public decimal? CostSwc => item.CostSwc;
    public string Category => item.Category;
    public string? WikiUrl => item.WikiUrl;
    public string? WikiSection => item.WikiSection;

    public override string CostDisplay => SeasonEconomy.PriceText(CostCr, CostSwc);
}
