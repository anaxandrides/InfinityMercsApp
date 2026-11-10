namespace InfinityMercsApp.Domain.Models.Army.SpecOps;

/// <summary>One upgrade a Spec-Ops or Team-Ops trooper can pick from its table, with everything it grants.</summary>
public sealed record SpecUpgrade(IReadOnlyList<SpecUpgradePart> Parts);
