using System.Net;
using InfinityMercsApp.Api.Cosmos;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;
using Microsoft.Azure.Cosmos;

namespace InfinityMercsApp.Api.ArmyData;

/// <inheritdoc/>
/// <remarks>
/// Keeps each snapshot as a document under its id in the army container, and the current one again under
/// <see cref="LatestId"/>.
/// </remarks>
public sealed class CosmosArmySnapshotStore(CosmosClient cosmos) : IArmySnapshotStore
{
    public const string LatestId = "latest";

    private static readonly PartitionKey Partition = new(CosmosNames.ArmyPartition);

    private readonly Container _container = cosmos.GetContainer(CosmosNames.Database, CosmosNames.ArmyContainer);

    /// <inheritdoc/>
    public Task<ArmyMirrorManifest?> ReadLatestAsync(CancellationToken cancellationToken = default) => ReadDocumentAsync(LatestId, cancellationToken);

    /// <inheritdoc/>
    public Task<ArmyMirrorManifest?> ReadAsync(string snapshotId, CancellationToken cancellationToken = default) =>
        ReadDocumentAsync(snapshotId, cancellationToken);

    /// <inheritdoc/>
    public Task SaveAsync(ArmyMirrorManifest snapshot, CancellationToken cancellationToken = default) =>
        _container.UpsertItemAsync(new ArmySnapshotDocument(snapshot.SnapshotId, CosmosNames.ArmyPartition, snapshot), Partition, cancellationToken: cancellationToken);

    /// <inheritdoc/>
    public Task SetLatestAsync(ArmyMirrorManifest manifest, CancellationToken cancellationToken = default) =>
        _container.UpsertItemAsync(new ArmySnapshotDocument(LatestId, CosmosNames.ArmyPartition, manifest), Partition, cancellationToken: cancellationToken);

    private async Task<ArmyMirrorManifest?> ReadDocumentAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _container.ReadItemAsync<ArmySnapshotDocument>(id, Partition, cancellationToken: cancellationToken);
            return response.Resource.Manifest;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
