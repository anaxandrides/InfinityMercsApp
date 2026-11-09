using System.Net;
using InfinityMercsApp.Api.Cosmos;
using InfinityMercsApp.Api.Leagues;
using InfinityMercsApp.Api.UnitTests.Cosmos;
using Microsoft.Azure.Cosmos;
using NSubstitute;
using static InfinityMercsApp.Api.UnitTests.Leagues.Leagues;

namespace InfinityMercsApp.Api.UnitTests.Leagues;

/// <summary>Each league is one document in its own partition; invites are read by code.</summary>
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
    public async Task TryReplaceAsync_ChangedSinceRead_IsFalse()
    {
        _leagues.ReplaceItemAsync(Arg.Any<LeagueDocument>(), Arg.Any<string>(), Arg.Any<PartitionKey?>(), Arg.Any<ItemRequestOptions>(), Arg.Any<CancellationToken>())
            .Returns<ItemResponse<LeagueDocument>>(_ => throw CosmosFailures.With(HttpStatusCode.PreconditionFailed));

        Assert.False(await _store.TryReplaceAsync(Sample(), "etag-1"));
    }

    [Fact]
    public async Task TryReplaceAsync_Unchanged_ReplacesOnItsETag()
    {
        Assert.True(await _store.TryReplaceAsync(Sample(), "etag-1"));

        await _leagues.Received(1).ReplaceItemAsync(
            Arg.Any<LeagueDocument>(), "league-1", new PartitionKey("league-1"), Arg.Is<ItemRequestOptions>(options => options.IfMatchEtag == "etag-1"), Arg.Any<CancellationToken>());
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
