using System.Net;
using InfinityMercsApp.Api.Cosmos;
using Microsoft.Azure.Cosmos;

namespace InfinityMercsApp.Api.Leagues;

/// <inheritdoc/>
/// <remarks>Each league is one document in the leagues container, in its own partition; invites are kept by code.</remarks>
public sealed class CosmosLeagueStore(CosmosClient cosmos) : ILeagueStore
{
    private readonly Container _leagues = cosmos.GetContainer(CosmosNames.Database, CosmosNames.LeaguesContainer);
    private readonly Container _invites = cosmos.GetContainer(CosmosNames.Database, CosmosNames.LeagueInvitesContainer);

    /// <inheritdoc/>
    public async Task<StoredLeague?> ReadAsync(string leagueId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _leagues.ReadItemAsync<LeagueDocument>(leagueId, new PartitionKey(leagueId), cancellationToken: cancellationToken);
            return new StoredLeague(response.Resource, response.ETag);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    /// <remarks>Searches every league's partition, which is fine while there are few leagues.</remarks>
    public async Task<IReadOnlyList<LeagueDocument>> ListForMemberAsync(string userId, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(c.memberUserIds, @user)").WithParameter("@user", userId);
        using var pages = _leagues.GetItemQueryIterator<LeagueDocument>(query);
        var leagues = new List<LeagueDocument>();
        while (pages.HasMoreResults)
        {
            leagues.AddRange(await pages.ReadNextAsync(cancellationToken));
        }

        return leagues;
    }

    /// <inheritdoc/>
    public async Task<bool> TryCreateAsync(LeagueDocument league, CancellationToken cancellationToken = default)
    {
        try
        {
            await _leagues.CreateItemAsync(league, new PartitionKey(league.LeagueId), cancellationToken: cancellationToken);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> TryReplaceAsync(LeagueDocument league, string etag, CancellationToken cancellationToken = default)
    {
        try
        {
            await _leagues.ReplaceItemAsync(league, league.Id, new PartitionKey(league.LeagueId), new ItemRequestOptions { IfMatchEtag = etag }, cancellationToken);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> FindInviteAsync(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _invites.ReadItemAsync<LeagueInvite>(code, new PartitionKey(code), cancellationToken: cancellationToken);
            return response.Resource.LeagueId;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> TryCreateInviteAsync(LeagueInvite invite, CancellationToken cancellationToken = default)
    {
        try
        {
            await _invites.CreateItemAsync(invite, new PartitionKey(invite.Code), cancellationToken: cancellationToken);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteInviteAsync(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            await _invites.DeleteItemAsync<LeagueInvite>(code, new PartitionKey(code), cancellationToken: cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Already gone.
        }
    }
}
