namespace InfinityMercsApp.Domain.Models.Army;

/// <summary>
/// One spec-ops choice: a skill, piece of equipment or weapon the spec-ops trooper can buy for
/// <see cref="Exp"/> experience, with its extras and anything it grants alongside.
/// </summary>
public sealed record SpecopsChoice(
    int Id,
    int Exp,
    IReadOnlyList<int> Extras,
    IReadOnlyList<SpecopsLink> Skills,
    IReadOnlyList<SpecopsLink> Equip,
    IReadOnlyList<SpecopsLink> Weapons);

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
