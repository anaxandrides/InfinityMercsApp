namespace InfinityMercsApp.Domain.Models.Army.SpecOps;

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
