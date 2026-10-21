namespace InfinityMercsApp.Api.ArmyData;

/// <summary>What one check of Corvus Belli changed in the mirror.</summary>
/// <param name="SnapshotId">The current snapshot after the check.</param>
/// <param name="NewSnapshot">Whether the check took a new snapshot, because some army data changed.</param>
/// <param name="MetadataUpdated">Whether the metadata changed.</param>
/// <param name="UpdatedFactionIds">The armies copied because they changed or were new.</param>
/// <param name="FailedFactionIds">The armies that couldn't be downloaded; their earlier copy, if any, is kept.</param>
public sealed record ArmyMirrorRefresh(
    string SnapshotId,
    bool NewSnapshot,
    bool MetadataUpdated,
    IReadOnlyList<int> UpdatedFactionIds,
    IReadOnlyList<int> FailedFactionIds);
