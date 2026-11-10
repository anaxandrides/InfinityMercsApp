using System.Net;
using InfinityMercsApp.Api.Cosmos;
using Microsoft.Azure.Cosmos;

namespace InfinityMercsApp.Api.Leagues;

/// <inheritdoc/>
/// <remarks>
/// Each league has its own partition, holding the league document and a document for each entry and game; writes to
/// one league go in a transactional batch. Invites are kept by code.
/// </remarks>
public sealed class CosmosLeagueStore(CosmosClient cosmos) : ILeagueStore
{
    private readonly Container _leagues = cosmos.GetContainer(CosmosNames.Database, CosmosNames.LeaguesContainer);
    private readonly Container _invites = cosmos.GetContainer(CosmosNames.Database, CosmosNames.LeagueInvitesContainer);

    /// <inheritdoc/>
    public Task<StoredDocument<LeagueDocument>?> ReadAsync(string leagueId, CancellationToken cancellationToken = default) =>
        ReadItemAsync<LeagueDocument>(leagueId, leagueId, cancellationToken);

    /// <inheritdoc/>
    /// <remarks>Searches every league's partition, which is fine while there are few leagues.</remarks>
    public Task<IReadOnlyList<LeagueDocument>> ListForMemberAsync(string userId, CancellationToken cancellationToken = default) =>
        QueryAsync<LeagueDocument>(
            new QueryDefinition("SELECT * FROM c WHERE c.kind = @kind AND ARRAY_CONTAINS(c.memberUserIds, @user)")
                .WithParameter("@kind", LeagueItemKinds.League)
                .WithParameter("@user", userId),
            null,
            cancellationToken);

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
    public Task<StoredDocument<LeagueEntry>?> ReadEntryAsync(string leagueId, string seasonId, CancellationToken cancellationToken = default) =>
        ReadItemAsync<LeagueEntry>(LeagueEntry.IdFor(seasonId), leagueId, cancellationToken);

    /// <inheritdoc/>
    public Task<StoredDocument<LeagueGame>?> ReadGameAsync(string leagueId, string gameId, CancellationToken cancellationToken = default) =>
        ReadItemAsync<LeagueGame>(LeagueGame.IdFor(gameId), leagueId, cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<LeagueEntry>> ListEntriesAsync(string leagueId, int leagueSeason, CancellationToken cancellationToken = default) =>
        QueryAsync<LeagueEntry>(InSeason(LeagueItemKinds.Entry, leagueSeason), leagueId, cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<LeagueGame>> ListGamesAsync(string leagueId, int leagueSeason, CancellationToken cancellationToken = default) =>
        QueryAsync<LeagueGame>(InSeason(LeagueItemKinds.Game, leagueSeason), leagueId, cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> ListEnteredSeasonIdsAsync(string leagueId, string memberId, CancellationToken cancellationToken = default) =>
        QueryAsync<string>(
            new QueryDefinition("SELECT VALUE c.seasonId FROM c WHERE c.kind = @kind AND c.memberId = @member")
                .WithParameter("@kind", LeagueItemKinds.Entry)
                .WithParameter("@member", memberId),
            leagueId,
            cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> TryCommitAsync(string leagueId, LeagueWrites writes, CancellationToken cancellationToken = default)
    {
        var batch = _leagues.CreateTransactionalBatch(new PartitionKey(leagueId));
        if (writes.League is { } league)
        {
            Add(batch, league);
        }

        foreach (var entry in writes.Entries)
        {
            Add(batch, entry);
        }

        foreach (var game in writes.Games)
        {
            Add(batch, game);
        }

        using var response = await batch.ExecuteAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        // The batch fails with the status of the write that failed: one changed, already kept, or gone since it was read.
        return response.StatusCode is HttpStatusCode.PreconditionFailed or HttpStatusCode.Conflict or HttpStatusCode.NotFound
            ? false
            : throw new InvalidOperationException($"Cosmos DB refused the league's writes: {(int)response.StatusCode} {response.ErrorMessage}");
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

    private static QueryDefinition InSeason(string kind, int leagueSeason) =>
        new QueryDefinition("SELECT * FROM c WHERE c.kind = @kind AND c.leagueSeason = @season")
            .WithParameter("@kind", kind)
            .WithParameter("@season", leagueSeason);

    private static void Add<T>(TransactionalBatch batch, DocumentWrite<T> write)
        where T : ILeagueItem
    {
        var options = new TransactionalBatchItemRequestOptions { IfMatchEtag = write.ETag };
        switch (write.Kind)
        {
            case DocumentWriteKind.Create:
                batch.CreateItem(write.Document);
                break;
            case DocumentWriteKind.Replace:
                batch.ReplaceItem(write.Document.Id, write.Document, options);
                break;
            default:
                batch.DeleteItem(write.Document.Id, options);
                break;
        }
    }

    private async Task<StoredDocument<T>?> ReadItemAsync<T>(string id, string leagueId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _leagues.ReadItemAsync<T>(id, new PartitionKey(leagueId), cancellationToken: cancellationToken);
            return new StoredDocument<T>(response.Resource, response.ETag);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <param name="leagueId">The league whose partition is searched; null searches them all.</param>
    private async Task<IReadOnlyList<T>> QueryAsync<T>(QueryDefinition query, string? leagueId, CancellationToken cancellationToken)
    {
        var options = leagueId is null ? null : new QueryRequestOptions { PartitionKey = new PartitionKey(leagueId) };
        using var pages = _leagues.GetItemQueryIterator<T>(query, requestOptions: options);
        var items = new List<T>();
        while (pages.HasMoreResults)
        {
            items.AddRange(await pages.ReadNextAsync(cancellationToken));
        }

        return items;
    }
}
