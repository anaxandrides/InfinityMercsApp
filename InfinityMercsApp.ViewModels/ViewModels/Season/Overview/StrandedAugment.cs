using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.Inventory;

namespace InfinityMercsApp.ViewModels.Season.Overview;

/// <summary>An equipped augment a gear change would leave without its requirement, which goes back to the inventory.</summary>
public sealed record StrandedAugment(int EntryIndex, string TrooperName, GearSlot Slot, string ItemName, AugmentRequirement Requirement)
{
    /// <summary>E.g. "Bolt's Armed Turret (MMR) (Needs Engineer)".</summary>
    public string Describe() => $"{TrooperName}'s {ItemName} ({Requirement.Unmet})";
}
