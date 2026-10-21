namespace InfinityMercsApp.UnitTests.Characterization;

/// <summary>
/// One unit's stored JSON blobs, exactly as the app keeps them (the SQLite <c>units</c> table columns),
/// plus the owning army's <c>filters</c> blob used to build id → name lookups.
/// </summary>
internal sealed record CorpusUnit(
    string Key,
    string Source,
    int FactionId,
    string ProfileGroupsJson,
    string? FiltersJson,
    string? FactionsJson,
    string? ArmyFiltersJson)
{
    public override string ToString() => Key;
}
