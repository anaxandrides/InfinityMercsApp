using InfinityMercsApp.Domain.Models.Stores;

namespace InfinityMercsApp.ViewModels;

public sealed class AugmentRow(StoreAugment augment) : MarketplaceRow(augment.Name, augment.CostCr)
{
    public string Name => augment.Name;
    public string? Requirement => augment.Requirement;
    public bool HasRequirement => !string.IsNullOrWhiteSpace(Requirement);
    public string RequirementDisplay => Requirement ?? "—";
    public string CostNote => augment.CostNote ?? string.Empty;
}
