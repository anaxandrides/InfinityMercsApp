namespace InfinityMercsApp.Domain.Models.Army.Filters;

/// <summary>
/// A unit's own <c>filters</c> payload: the ids of everything the unit can field, per section.
/// </summary>
public sealed class UnitFilterIds
{
    public static UnitFilterIds Empty { get; } = new();

    public IReadOnlyList<int> Categories { get; set; } = [];

    public IReadOnlyList<int> Types { get; set; } = [];

    public IReadOnlyList<int> Chars { get; set; } = [];

    public IReadOnlyList<int> Skills { get; set; } = [];

    public IReadOnlyList<int> Equip { get; set; } = [];

    public IReadOnlyList<int> Weapons { get; set; } = [];

    public IReadOnlyList<int> Ammunition { get; set; } = [];
}
