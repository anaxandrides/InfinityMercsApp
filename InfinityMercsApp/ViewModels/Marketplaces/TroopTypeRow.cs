using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season.Inventory;

namespace InfinityMercsApp.ViewModels.Marketplaces;

/// <summary>A troop type's armour, at its own price or at <paramref name="costCr"/> when the price is adjusted.</summary>
public sealed class TroopTypeRow(StoreTroopType troopType, int? costCr = null) : MarketplaceRow(InventoryCatalog.ArmorName(troopType), costCr ?? troopType.CostCr)
{
    public string DisplayName => CartName;
    public string ArmDisplay => troopType.Arm?.ToString() ?? "—";
    public string BtsDisplay => troopType.Bts?.ToString() ?? "—";
    public string Abilities => troopType.Abilities ?? string.Empty;
    public bool HasAbilities => !string.IsNullOrWhiteSpace(Abilities);
    public string StatDisplay => $"{Stat.Arm.Key()} {ArmDisplay}  |  {Stat.Bts.Key()} {BtsDisplay}";
    public override string InventoryCategory => GearSlot.Armor.Key();
}
