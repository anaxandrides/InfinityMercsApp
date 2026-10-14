using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Models.Stores;

namespace InfinityMercsApp.ViewModels.Marketplaces;

public sealed class TroopTypeRow(StoreTroopType troopType) : MarketplaceRow(DisplayNameFor(troopType), troopType.CostCr)
{
    public string DisplayName => CartName;
    public string ArmDisplay => troopType.Arm?.ToString() ?? "—";
    public string BtsDisplay => troopType.Bts?.ToString() ?? "—";
    public string Abilities => troopType.Abilities ?? string.Empty;
    public bool HasAbilities => !string.IsNullOrWhiteSpace(Abilities);
    public string StatDisplay => $"{Stat.Arm.Key()} {ArmDisplay}  |  {Stat.Bts.Key()} {BtsDisplay}";

    // "Light Armour (LI)", or just the armour when the troop type is not named.
    private static string DisplayNameFor(StoreTroopType troopType) =>
        string.IsNullOrWhiteSpace(troopType.Type) ? troopType.ArmorName : $"{troopType.ArmorName} ({troopType.Type})";
}
