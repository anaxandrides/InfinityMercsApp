using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Models.Stores;

namespace InfinityMercsApp.ViewModels.Marketplaces.Items;

/// <summary>An augment, at its own price or at <paramref name="costCr"/> when the price is adjusted.</summary>
public sealed class AugmentRow(StoreAugment augment, int? costCr = null) : MarketplaceRow(augment.Name, costCr ?? augment.CostCr)
{
    public string Name => augment.Name;
    public string? Requirement => augment.Requirement;
    public bool HasRequirement => !string.IsNullOrWhiteSpace(Requirement);
    public string RequirementDisplay => Requirement ?? "—";
    public string CostNote => augment.CostNote ?? string.Empty;
    public override string InventoryCategory => GearSlot.Augments.Key();
}
