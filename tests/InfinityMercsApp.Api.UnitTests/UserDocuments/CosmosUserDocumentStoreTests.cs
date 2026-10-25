using System.Net;
using InfinityMercsApp.Api.Cosmos;
using InfinityMercsApp.Api.UnitTests.Cosmos;
using InfinityMercsApp.Api.UserDocuments;
using InfinityMercsApp.ApiClient.UserDocuments;
using Microsoft.Azure.Cosmos;
using NSubstitute;

namespace InfinityMercsApp.Api.UnitTests.UserDocuments;

/// <summary>Each user's files are kept in their own partition, and replaced only from the version read.</summary>
public sealed class CosmosUserDocumentStoreTests
{
    private const string User = "user-1";

    private readonly Container _container = Substitute.For<Container>();
    private readonly CosmosUserDocumentStore _store;

    public CosmosUserDocumentStoreTests()
    {
        var cosmos = Substitute.For<CosmosClient>();
        cosmos.GetContainer(CosmosNames.Database, CosmosNames.UserDataContainer).Returns(_container);
        _store = new CosmosUserDocumentStore(cosmos);
    }

    private static UserDocument<string> Document(string documentId, int revision = 1) =>
        new(UserDocumentIds.For(UserDocumentKind.Company, documentId), User, UserDocumentKind.Company, documentId, revision, DateTimeOffset.UnixEpoch, false, null, "content");

    [Fact]
    public async Task ListAsync_SeveralPages_ReadsThemAll()
    {
        var first = Page(Document("c1"));
        var second = Page(Document("c2"));
        var pages = Substitute.For<FeedIterator<UserDocument<string>>>();
        pages.HasMoreResults.Returns(true, true, false);
        pages.ReadNextAsync(Arg.Any<CancellationToken>()).Returns(first, second);
        _container.GetItemQueryIterator<UserDocument<string>>(
                Arg.Any<QueryDefinition>(), Arg.Any<string>(), Arg.Is<QueryRequestOptions>(options => options.PartitionKey == new PartitionKey(User)))
            .Returns(pages);

        var documents = await _store.ListAsync<string>(User, UserDocumentKind.Company);

        Assert.Equal(["c1", "c2"], documents.Select(document => document.DocumentId));
    }

    [Fact]
    public async Task ReadAsync_Kept_ReturnsItWithItsETag()
    {
        var document = Document("c1");
        var response = Substitute.For<ItemResponse<UserDocument<string>>>();
        response.Resource.Returns(document);
        response.ETag.Returns("etag-1");
        _container.ReadItemAsync<UserDocument<string>>(UserDocumentIds.For(UserDocumentKind.Company, "c1"), new PartitionKey(User), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(response);

        var stored = await _store.ReadAsync<string>(User, UserDocumentKind.Company, "c1");

        Assert.Equal("c1", stored?.Document.DocumentId);
        Assert.Equal("etag-1", stored?.ETag);
    }

    [Fact]
    public async Task ReadAsync_NotKept_IsNull()
    {
        _container.ReadItemAsync<UserDocument<string>>(Arg.Any<string>(), Arg.Any<PartitionKey>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns<ItemResponse<UserDocument<string>>>(_ => throw CosmosFailures.With(HttpStatusCode.NotFound));

        Assert.Null(await _store.ReadAsync<string>(User, UserDocumentKind.Company, "c1"));
    }

    [Fact]
    public async Task TryCreateAsync_New_IsTrue() => Assert.True(await _store.TryCreateAsync(Document("c1")));

    [Fact]
    public async Task TryCreateAsync_AlreadyKept_IsFalse()
    {
        _container.CreateItemAsync(Arg.Any<UserDocument<string>>(), Arg.Any<PartitionKey?>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns<ItemResponse<UserDocument<string>>>(_ => throw CosmosFailures.With(HttpStatusCode.Conflict));

        Assert.False(await _store.TryCreateAsync(Document("c1")));
    }

    [Fact]
    public async Task TryReplaceAsync_SameVersion_ReplacesIfItMatches()
    {
        var document = Document("c1", revision: 2);

        Assert.True(await _store.TryReplaceAsync(document, "etag-1"));

        await _container.Received(1).ReplaceItemAsync(
            document, document.Id, new PartitionKey(User), Arg.Is<ItemRequestOptions>(options => options.IfMatchEtag == "etag-1"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryReplaceAsync_ChangedSince_IsFalse()
    {
        _container.ReplaceItemAsync(Arg.Any<UserDocument<string>>(), Arg.Any<string>(), Arg.Any<PartitionKey?>(), Arg.Any<ItemRequestOptions>(), Arg.Any<CancellationToken>())
            .Returns<ItemResponse<UserDocument<string>>>(_ => throw CosmosFailures.With(HttpStatusCode.PreconditionFailed));

        Assert.False(await _store.TryReplaceAsync(Document("c1"), "etag-old"));
    }

    [Fact]
    public async Task TryReplaceAsync_CosmosDown_Throws()
    {
        _container.ReplaceItemAsync(Arg.Any<UserDocument<string>>(), Arg.Any<string>(), Arg.Any<PartitionKey?>(), Arg.Any<ItemRequestOptions>(), Arg.Any<CancellationToken>())
            .Returns<ItemResponse<UserDocument<string>>>(_ => throw CosmosFailures.With(HttpStatusCode.ServiceUnavailable));

        await Assert.ThrowsAsync<CosmosException>(() => _store.TryReplaceAsync(Document("c1"), "etag-1"));
    }

    private static FeedResponse<UserDocument<string>> Page(params IEnumerable<UserDocument<string>> documents)
    {
        var page = Substitute.For<FeedResponse<UserDocument<string>>>();
        page.GetEnumerator().Returns(_ => documents.ToList().GetEnumerator());
        return page;
    }
}
