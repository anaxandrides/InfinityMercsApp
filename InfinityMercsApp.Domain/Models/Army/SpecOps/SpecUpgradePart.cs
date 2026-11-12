namespace InfinityMercsApp.Domain.Models.Army.SpecOps;

/// <summary>
/// One part of an upgrade from a Spec-Ops or Team-Ops table. A <see cref="SpecUpgradePartKind.Stat"/> part names its
/// stat as the army data does (<c>"bts"</c>, <c>"move0"</c>) with <see cref="Quantity"/>; any other part is the
/// skill, equipment or weapon <see cref="Id"/> with its extras.
/// </summary>
public sealed record SpecUpgradePart(SpecUpgradePartKind Kind, int? Id, string? StatKey, int Quantity, IReadOnlyList<int> Extras)
{
    public static SpecUpgradePart ForStat(string statKey, int quantity) => new(SpecUpgradePartKind.Stat, null, statKey, quantity, []);

    public static SpecUpgradePart ForItem(SpecUpgradePartKind kind, int id, params IEnumerable<int> extras) =>
        new(kind, id, null, 1, extras.ToList());
}
