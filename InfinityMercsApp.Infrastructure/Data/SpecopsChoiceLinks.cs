using InfinityMercsApp.Domain.Models.Army;

namespace InfinityMercsApp.Infrastructure.Data;

/// <summary>What a spec-ops choice carries besides its id and cost: its extras and the items it grants.</summary>
public sealed record SpecopsChoiceLinks(
    IReadOnlyList<int> Extras,
    IReadOnlyList<SpecopsLink> Skills,
    IReadOnlyList<SpecopsLink> Equip,
    IReadOnlyList<SpecopsLink> Weapons)
{
    public static SpecopsChoiceLinks Empty { get; } = new([], [], [], []);

    public static SpecopsChoiceLinks From(SpecopsChoice choice) => new(choice.Extras, choice.Skills, choice.Equip, choice.Weapons);
}
