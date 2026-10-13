using InfinityMercsApp.Domain.Models.Army.Profiles;

namespace InfinityMercsApp.Domain.Models.Army;

public class SpecopsUnit
{
    public string SpecopsUnitKey { get; set; } = string.Empty;

    public int FactionId { get; set; }

    public int EntryOrder { get; set; }

    public int UnitId { get; set; }

    public int? IdArmy { get; set; }

    public int? Canonical { get; set; }

    public string? Isc { get; set; }

    public string? IscAbbr { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Slug { get; set; }

    public UnitProfileGroups ProfileGroups { get; set; } = UnitProfileGroups.Empty;

    /// <summary>Options declared on the unit itself, such as a two-trooper team option.</summary>
    public IReadOnlyList<ProfileOption> Options { get; set; } = [];

    public UnitFilterIds Filters { get; set; } = UnitFilterIds.Empty;

    /// <summary>The armies the unit belongs to.</summary>
    public IReadOnlyList<int> FactionIds { get; set; } = [];
}