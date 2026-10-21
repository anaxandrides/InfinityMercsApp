namespace InfinityMercsApp.Domain.Models.Army.SpecOps;

/// <summary>
/// An army's <c>specops</c> payload. A list is <c>null</c> when the payload does not send it at all (as
/// opposed to sending it empty), which lets JSA armies fall back to Yu Jing's choices.
/// </summary>
public sealed class SpecopsCatalog
{
    public static SpecopsCatalog Empty { get; } = new();

    public IReadOnlyList<SpecopsChoice>? Skills { get; init; }

    public IReadOnlyList<SpecopsChoice>? Equip { get; init; }

    public IReadOnlyList<SpecopsChoice>? Weapons { get; init; }

    public IReadOnlyList<ArmyUnitDefinition> Units { get; init; } = [];
}
