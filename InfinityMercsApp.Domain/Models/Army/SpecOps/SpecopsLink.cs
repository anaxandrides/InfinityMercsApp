namespace InfinityMercsApp.Domain.Models.Army.SpecOps;

/// <summary>
/// An item a spec-ops choice also grants (e.g. a skill that comes with a piece of equipment),
/// with the ids of that item's extras.
/// </summary>
public sealed record SpecopsLink(int Id, IReadOnlyList<int> Extras);
