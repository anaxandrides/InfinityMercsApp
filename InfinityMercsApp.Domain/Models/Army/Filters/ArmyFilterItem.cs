namespace InfinityMercsApp.Domain.Models.Army.Filters;

/// <summary>
/// One item of an army filter section.
/// </summary>
public sealed record ArmyFilterItem(int Id, string? Name, string? Type, string? Url)
{
    /// <summary>The item is flagged for MERCS (<c>"mercs": true</c>).</summary>
    public bool Mercs { get; init; }

    /// <summary>The item is flagged as a spec-ops choice (<c>"specops": true</c>).</summary>
    public bool SpecOps { get; init; }
}
