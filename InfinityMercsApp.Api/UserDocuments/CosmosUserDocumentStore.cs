using System.Net;
using InfinityMercsApp.Api.Cosmos;
using InfinityMercsApp.Infrastructure.API.UserDocuments;
using Microsoft.Azure.Cosmos;

namespace InfinityMercsApp.Api.UserDocuments;

/// <inheritdoc/>
/// <remarks>Keeps every user's files in the user-data container, partitioned by user.</remarks>
public sealed class CosmosUserDocumentStore(CosmosClient cosmos) : IUserDocumentStore
{
    private readonly Container _container = cosmos.GetContainer(CosmosNames.Database, CosmosNames.UserDataContainer);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<UserDocument<TContent>>> ListAsync<TContent>(string userId, UserDocumentKind kind, CancellationToken cancellationToken = default)
        where TContent : class
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.kind = @kind").WithParameter("@kind", kind.ToString());
        using var pages = _container.GetItemQueryIterator<UserDocument<TContent>>(
            query, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(userId) });
        var documents = new List<UserDocument<TContent>>();
        while (pages.HasMoreResults)
        {
            documents.AddRange(await pages.ReadNextAsync(cancellationToken));
        }

        return documents;
    }

    /// <inheritdoc/>
    public async Task<StoredUserDocument<TContent>?> ReadAsync<TContent>(
        string userId,
        UserDocumentKind kind,
        string documentId,
        CancellationToken cancellationToken = default)
        where TContent : class
    {
        try
        {
            var response = await _container.ReadItemAsync<UserDocument<TContent>>(
                UserDocumentIds.For(kind, documentId), new PartitionKey(userId), cancellationToken: cancellationToken);
            return new StoredUserDocument<TContent>(response.Resource, response.ETag);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> TryCreateAsync<TContent>(UserDocument<TContent> document, CancellationToken cancellationToken = default)
        where TContent : class
    {
        try
        {
            await _container.CreateItemAsync(document, new PartitionKey(document.UserId), cancellationToken: cancellationToken);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> TryReplaceAsync<TContent>(UserDocument<TContent> document, string etag, CancellationToken cancellationToken = default)
        where TContent : class
    {
        try
        {
            await _container.ReplaceItemAsync(
                document,
                document.Id,
                new PartitionKey(document.UserId),
                new ItemRequestOptions { IfMatchEtag = etag },
                cancellationToken);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            return false;
        }
    }
}
