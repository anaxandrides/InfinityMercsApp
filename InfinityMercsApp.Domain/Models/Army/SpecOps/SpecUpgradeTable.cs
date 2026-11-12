namespace InfinityMercsApp.Domain.Models.Army.SpecOps;

/// <summary>
/// The upgrades a Spec-Ops or Team-Ops unit picks from when it is built (the army data's <c>spectables.table</c>).
/// Only those units have one, so a unit with upgrades is one of them.
/// </summary>
public sealed record SpecUpgradeTable(IReadOnlyList<SpecUpgrade> Upgrades)
{
    public static SpecUpgradeTable Empty { get; } = new([]);

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsEmpty => Upgrades.Count == 0;
}
