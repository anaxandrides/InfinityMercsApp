using System.Net;
using InfinityMercsApp.Api.Cosmos;
using InfinityMercsApp.Api.Leagues;
using InfinityMercsApp.Api.UnitTests.Cosmos;
using Microsoft.Azure.Cosmos;
using NSubstitute;
using static InfinityMercsApp.Api.UnitTests.Leagues.Leagues;

namespace InfinityMercsApp.Api.UnitTests.Leagues;

/// <summary>Each league has its own partition, written in transactional batches; invites are read by code.</summary>
public sealed class CosmosLeagueStoreTests
{
    private readonly Container _leagues = Substitute.For<Container>();
    private readonly Container _invites = Substitute.For<Container>();
    private readonly CosmosLeagueStore _store;

    public CosmosLeagueStoreTests()
    {
        var cosmos = Substitute.For<CosmosClient>();
        cosmos.GetContainer(CosmosNames.Database, CosmosNames.LeaguesContainer).Returns(_leagues);
        cosmos.GetContainer(CosmosNames.Database, CosmosNames.LeagueInvitesContainer).Returns(_invites);
        _store = new CosmosLeagueStore(cosmos);
    }

    [Fact]
    public async Task ReadAsync_Kept_ReturnsItWithItsETag()
    {
        var response = Substitute.For<ItemResponse<LeagueDocument>>();
        response.Resource.Returns(Sample());
        response.ETag.Returns("etag-1");
        _leagues.ReadItemAsync<LeagueDocument>("league-1", new PartitionKey("league-1"), cancellationToken: Arg.Any<CancellationToken>()).Returns(response);

        var stored = await _store.ReadAsync("league-1");

        Assert.Equal("etag-1", stored?.ETag);
    }

    [Fact]
    public async Task ReadAsync_NotKept_IsNull()
    {
        _leagues.ReadItemAsync<LeagueDocument>(Arg.Any<string>(), Arg.Any<PartitionKey>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns<ItemResponse<LeagueDocument>>(_ => throw CosmosFailures.With(HttpStatusCode.NotFound));

        Assert.Null(await _store.ReadAsync("league-1"));
    }

    [Fact]
    public async Task ListForMemberAsync_SeveralPages_ReadsThemAll()
    {
        var page = Substitute.For<FeedResponse<LeagueDocument>>();
        page.GetEnumerator().Returns(_ => new List<LeagueDocument> { Sample() }.GetEnumerator());
        var pages = Substitute.For<FeedIterator<LeagueDocument>>();
        pages.HasMoreResults.Returns(true, true, false);
        pages.ReadNextAsync(Arg.Any<CancellationToken>()).Returns(page);
        _leagues.GetItemQueryIterator<LeagueDocument>(Arg.Any<QueryDefinition>(), Arg.Any<string>(), Arg.Any<QueryRequestOptions>()).Returns(pages);

        Assert.Equal(2, (await _store.ListForMemberAsync(Member)).Count);
    }

    [Fact]
    public async Task TryCreateAsync_AlreadyKept_IsFalse()
    {
        _leagues.CreateItemAsync(Arg.Any<LeagueDocument>(), Arg.Any<PartitionKey?>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns<ItemResponse<LeagueDocument>>(_ => throw CosmosFailures.With(HttpStatusCode.Conflict));

        Assert.False(await _store.TryCreateAsync(Sample()));
    }

    [Fact]
    public async Task TryCreateAsync_New_IsTrue() => Assert.True(await _store.TryCreateAsync(Sample()));

    [Fact]
    public async Task ReadEntryAsync_Kept_ReadsItByItsIdInTheLeaguesPartition()
    {
        var response = Substitute.For<ItemResponse<LeagueEntry>>();
        response.Resource.Returns(MemberEntry);
        response.ETag.Returns("etag-2");
        _leagues.ReadItemAsync<LeagueEntry>("entry-season-m", new PartitionKey("league-1"), cancellationToken: Arg.Any<CancellationToken>()).Returns(response);

        var stored = await _store.ReadEntryAsync("league-1", MemberSeason);

        Assert.Equal("etag-2", stored?.ETag);
        Assert.Equal("Blue Company", stored?.Document.CompanyName);
    }

    [Fact]
    public async Task ReadGameAsync_NotKept_IsNull()
    {
        _leagues.ReadItemAsync<LeagueGame>(Arg.Any<string>(), Arg.Any<PartitionKey>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns<ItemResponse<LeagueGame>>(_ => throw CosmosFailures.With(HttpStatusCode.NotFound));

        Assert.Null(await _store.ReadGameAsync("league-1", "g1"));
    }

    [Fact]
    public async Task ListGamesAsync_AnySeason_SearchesOnlyTheLeaguesPartition()
    {
        var pages = Substitute.For<FeedIterator<LeagueGame>>();
        pages.HasMoreResults.Returns(false);
        _leagues.GetItemQueryIterator<LeagueGame>(Arg.Any<QueryDefinition>(), Arg.Any<string>(), Arg.Any<QueryRequestOptions>()).Returns(pages);

        await _store.ListGamesAsync("league-1", 2);

        _leagues.Received(1).GetItemQueryIterator<LeagueGame>(
            Arg.Any<QueryDefinition>(), Arg.Any<string>(), Arg.Is<QueryRequestOptions>(options => options.PartitionKey == new PartitionKey("league-1")));
    }

    [Fact]
    public async Task TryCommitAsync_Accepted_WritesEachDocumentInOneBatch()
    {
        var batch = Batch(HttpStatusCode.OK);
        var game = Recorded("g1", "m-m");
        var writes = new LeagueWrites(
            DocumentWrites.Replace(Sample(), "etag-1"),
            [DocumentWrites.Replace(MemberEntry, "etag-2")],
            [DocumentWrites.Create(game), DocumentWrites.Delete(Recorded("g0", "m-m"), "etag-3")]);

        Assert.True(await _store.TryCommitAsync("league-1", writes));

        batch.Received(1).ReplaceItem("league-1", Arg.Any<LeagueDocument>(), Arg.Is<TransactionalBatchItemRequestOptions>(options => options.IfMatchEtag == "etag-1"));
        batch.Received(1).ReplaceItem("entry-season-m", Arg.Any<LeagueEntry>(), Arg.Is<TransactionalBatchItemRequestOptions>(options => options.IfMatchEtag == "etag-2"));
        batch.Received(1).CreateItem(game, Arg.Any<TransactionalBatchItemRequestOptions>());
        batch.Received(1).DeleteItem("game-g0", Arg.Is<TransactionalBatchItemRequestOptions>(options => options.IfMatchEtag == "etag-3"));
        await batch.Received(1).ExecuteAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task TryCommitAsync_DocumentChangedSinceRead_IsFalse(HttpStatusCode status)
    {
        Batch(status);

        Assert.False(await _store.TryCommitAsync("league-1", LeagueWrites.Of(DocumentWrites.Replace(Sample(), "etag-1"))));
    }

    [Fact]
    public async Task TryCommitAsync_CosmosFails_Throws()
    {
        Batch(HttpStatusCode.ServiceUnavailable);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _store.TryCommitAsync("league-1", LeagueWrites.Of(DocumentWrites.Replace(Sample(), "etag-1"))));
    }

    private TransactionalBatch Batch(HttpStatusCode status)
    {
        var response = Substitute.For<TransactionalBatchResponse>();
        response.StatusCode.Returns(status);
        response.IsSuccessStatusCode.Returns((int)status is >= 200 and < 300);
        var batch = Substitute.For<TransactionalBatch>();
        batch.ExecuteAsync(Arg.Any<CancellationToken>()).Returns(response);
        _leagues.CreateTransactionalBatch(new PartitionKey("league-1")).Returns(batch);
        return batch;
    }

    [Fact]
    public async Task FindInviteAsync_Kept_ReturnsItsLeague()
    {
        var response = Substitute.For<ItemResponse<LeagueInvite>>();
        response.Resource.Returns(LeagueInvite.For("ABC234", "league-1"));
        _invites.ReadItemAsync<LeagueInvite>("ABC234", new PartitionKey("ABC234"), cancellationToken: Arg.Any<CancellationToken>()).Returns(response);

        Assert.Equal("league-1", await _store.FindInviteAsync("ABC234"));
    }

    [Fact]
    public async Task FindInviteAsync_NotKept_IsNull()
    {
        _invites.ReadItemAsync<LeagueInvite>(Arg.Any<string>(), Arg.Any<PartitionKey>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns<ItemResponse<LeagueInvite>>(_ => throw CosmosFailures.With(HttpStatusCode.NotFound));

        Assert.Null(await _store.FindInviteAsync("ABC234"));
    }

    [Fact]
    public async Task TryCreateInviteAsync_CodeTaken_IsFalse()
    {
        _invites.CreateItemAsync(Arg.Any<LeagueInvite>(), Arg.Any<PartitionKey?>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns<ItemResponse<LeagueInvite>>(_ => throw CosmosFailures.With(HttpStatusCode.Conflict));

        Assert.False(await _store.TryCreateInviteAsync(LeagueInvite.For("ABC234", "league-1")));
    }

    [Fact]
    public async Task TryCreateInviteAsync_New_IsTrue() => Assert.True(await _store.TryCreateInviteAsync(LeagueInvite.For("ABC234", "league-1")));

    [Fact]
    public async Task DeleteInviteAsync_AlreadyGone_DoesNotThrow()
    {
        _invites.DeleteItemAsync<LeagueInvite>(Arg.Any<string>(), Arg.Any<PartitionKey>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns<ItemResponse<LeagueInvite>>(_ => throw CosmosFailures.With(HttpStatusCode.NotFound));

        await _store.DeleteInviteAsync("ABC234");

        await _invites.Received(1).DeleteItemAsync<LeagueInvite>("ABC234", new PartitionKey("ABC234"), cancellationToken: Arg.Any<CancellationToken>());
    }
}
