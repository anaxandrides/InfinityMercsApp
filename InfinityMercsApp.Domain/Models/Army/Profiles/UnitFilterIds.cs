namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>
/// A unit's own <c>filters</c> payload: the ids of everything the unit can field, per section.
/// </summary>
public sealed class UnitFilterIds
{
    public static UnitFilterIds Empty { get; } = new();

    public IReadOnlyList<int> Categories { get; init; } = [];

    public IReadOnlyList<int> Types { get; init; } = [];

    public IReadOnlyList<int> Chars { get; init; } = [];

    public IReadOnlyList<int> Skills { get; init; } = [];

    public IReadOnlyList<int> Equip { get; init; } = [];

    public IReadOnlyList<int> Weapons { get; init; } = [];

    public IReadOnlyList<int> Ammunition { get; init; } = [];
}
