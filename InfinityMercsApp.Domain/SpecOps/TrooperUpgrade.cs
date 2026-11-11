using InfinityMercsApp.Domain.Captain;

namespace InfinityMercsApp.Domain.SpecOps;

/// <summary>
/// Something a trooper takes into its own profile as it joins a company, by name: a Spec-Ops or Team-Ops upgrade, or
/// gear bought with one-off credits. It may give stat bonuses and a new MOV, and add weapons, skills and equipment.
/// <see cref="Label"/> reads as the player picks it, e.g. "+1 ARM, Dogged"; <see cref="Source"/> says where it came
/// from, e.g. "Spec-Ops".
/// </summary>
public sealed record TrooperUpgrade(
    string Label,
    CaptainStatBonuses Bonuses,
    int? MoveFirstCm,
    int? MoveSecondCm,
    IReadOnlyList<string> Weapons,
    IReadOnlyList<string> Skills,
    IReadOnlyList<string> Equipment,
    string Source = SpecUpgradeRules.Source)
{
    /// <summary>Whether the upgrade sets the trooper's MOV.</summary>
    public bool SetsMove => MoveFirstCm is not null || MoveSecondCm is not null;

    /// <summary>What the trooper took, as an army sheet lists it among its upgrades: "Spec-Ops: +1 ARM, Dogged".</summary>
    public string PurchaseLine => $"{Source}: {Label}";
}
