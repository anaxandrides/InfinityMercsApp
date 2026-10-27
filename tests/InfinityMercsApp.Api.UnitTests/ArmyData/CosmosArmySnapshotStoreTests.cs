using System.Net;
using InfinityMercsApp.Api.ArmyData;
using InfinityMercsApp.Api.Cosmos;
using InfinityMercsApp.Api.UnitTests.Cosmos;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;
using Microsoft.Azure.Cosmos;
using NSubstitute;

namespace InfinityMercsApp.Api.UnitTests.ArmyData;

/// <summary>Each snapshot is kept under its id, and the current one again as "latest".</summary>
public sealed class CosmosArmySnapshotStoreTests
{
    private static readonly PartitionKey Partition = new(CosmosNames.ArmyPartition);

    private readonly Container _container = Substitute.For<Container>();
    private readonly CosmosArmySnapshotStore _store;

    public CosmosArmySnapshotStoreTests()
    {
        var cosmos = Substitute.For<CosmosClient>();
        cosmos.GetContainer(CosmosNames.Database, CosmosNames.ArmyContainer).Returns(_container);
        _store = new CosmosArmySnapshotStore(cosmos);
    }

    [Fact]
    public async Task ReadLatestAsync_Kept_ReturnsItsManifest()
    {
        var manifest = new ArmyMirrorManifest { SnapshotId = "s1" };
        var response = Substitute.For<ItemResponse<ArmySnapshotDocument>>();
        response.Resource.Returns(new ArmySnapshotDocument(CosmosArmySnapshotStore.LatestId, CosmosNames.ArmyPartition, manifest));
        _container.ReadItemAsync<ArmySnapshotDocument>(CosmosArmySnapshotStore.LatestId, Partition, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(response);

        Assert.Same(manifest, await _store.ReadLatestAsync());
    }

    [Fact]
    public async Task ReadAsync_NotKept_IsNull()
    {
        _container.ReadItemAsync<ArmySnapshotDocument>("s9", Partition, cancellationToken: Arg.Any<CancellationToken>())
            .Returns<ItemResponse<ArmySnapshotDocument>>(_ => throw CosmosFailures.With(HttpStatusCode.NotFound));

        Assert.Null(await _store.ReadAsync("s9"));
    }

    [Fact]
    public async Task SaveAsync_Snapshot_IsKeptUnderItsId()
    {
        var manifest = new ArmyMirrorManifest { SnapshotId = "s1" };

        await _store.SaveAsync(manifest);

        await _container.Received(1).UpsertItemAsync(
            Arg.Is<ArmySnapshotDocument>(document => document.Id == "s1" && document.Manifest == manifest), Partition, cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetLatestAsync_Snapshot_IsKeptAsLatest()
    {
        await _store.SetLatestAsync(new ArmyMirrorManifest { SnapshotId = "s1" });

        await _container.Received(1).UpsertItemAsync(
            Arg.Is<ArmySnapshotDocument>(document => document.Id == CosmosArmySnapshotStore.LatestId), Partition, cancellationToken: Arg.Any<CancellationToken>());
    }
}
