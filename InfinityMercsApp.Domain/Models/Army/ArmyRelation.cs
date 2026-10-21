namespace InfinityMercsApp.Domain.Models.Army;

/// <summary>
/// One of an army's unit <c>relations</c>: between <see cref="Min"/> and <see cref="Max"/> of the listed units
/// may be fielded together (e.g. a character and its alternative profile).
/// </summary>
public sealed record ArmyRelation(int? Min, int? Max, bool? IsGroup, IReadOnlyList<int> UnitIds);
