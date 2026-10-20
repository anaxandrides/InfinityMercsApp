namespace InfinityMercsApp.Api.ArmyData;

/// <summary>What one check of Corvus Belli changed in the mirror.</summary>
/// <param name="MetadataUpdated">Whether the metadata changed.</param>
/// <param name="UpdatedFactionIds">The armies copied because they changed or were new.</param>
/// <param name="FailedFactionIds">The armies that couldn't be downloaded; their earlier copy, if any, is kept.</param>
public sealed record ArmyMirrorRefresh(bool MetadataUpdated, IReadOnlyList<int> UpdatedFactionIds, IReadOnlyList<int> FailedFactionIds);
