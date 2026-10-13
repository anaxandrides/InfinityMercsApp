using InfinityMercsApp.Domain.Models.Army.Profiles;

namespace InfinityMercsApp.Domain.Models.Army;

/// <summary>
/// A unit as the Infinity Army API describes it (an army's <c>units[]</c> or <c>specops.units[]</c>),
/// with its profile groups, options, filters and factions already parsed into typed models.
/// </summary>
public sealed class ArmyUnitDefinition
{
    public int Id { get; init; }

    public int? IdArmy { get; init; }

    public int? Canonical { get; init; }

    public string? Isc { get; init; }

    public string? IscAbbr { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Slug { get; init; }

    public UnitProfileGroups ProfileGroups { get; init; } = UnitProfileGroups.Empty;

    /// <summary>Options declared on the unit itself, such as a two-trooper team option.</summary>
    public IReadOnlyList<ProfileOption> Options { get; init; } = [];

    public UnitFilterIds Filters { get; init; } = UnitFilterIds.Empty;

    /// <summary>The armies the unit belongs to.</summary>
    public IReadOnlyList<int> FactionIds { get; init; } = [];
}
