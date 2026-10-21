using InfinityMercsApp.Infrastructure.API.ArmyMirror;

namespace InfinityMercsApp.Api.ArmyData;

/// <summary>A snapshot as Cosmos DB keeps it.</summary>
/// <param name="Id">The snapshot's id, or <see cref="CosmosArmySnapshotStore.LatestId"/> for the current one.</param>
/// <param name="Partition">The partition key, always <see cref="Cosmos.CosmosNames.ArmyPartition"/>.</param>
public sealed record ArmySnapshotDocument(string Id, string Partition, ArmyMirrorManifest Manifest);
