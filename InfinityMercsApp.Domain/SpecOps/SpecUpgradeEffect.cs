using InfinityMercsApp.Domain.Captain;

namespace InfinityMercsApp.Domain.SpecOps;

/// <summary>
/// What one Spec-Ops or Team-Ops upgrade does to a trooper, by name: stat bonuses, a new MOV, and the weapons,
/// skills and equipment it adds. <see cref="Label"/> reads as the player picks it, e.g. "+1 ARM, Dogged".
/// </summary>
public sealed record SpecUpgradeEffect(
    string Label,
    CaptainStatBonuses Bonuses,
    int? MoveFirstCm,
    int? MoveSecondCm,
    IReadOnlyList<string> Weapons,
    IReadOnlyList<string> Skills,
    IReadOnlyList<string> Equipment)
{
    /// <summary>Whether the upgrade sets the trooper's MOV.</summary>
    public bool SetsMove => MoveFirstCm is not null || MoveSecondCm is not null;

    /// <summary>What the trooper took, as an army sheet lists it among its upgrades: "Spec-Ops: +1 ARM, Dogged".</summary>
    public string PurchaseLine => $"{SpecUpgradeRules.PurchasePrefix}{Label}";
}
