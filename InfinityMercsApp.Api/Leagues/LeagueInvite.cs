namespace InfinityMercsApp.Api.Leagues;

/// <summary>An invite code as Cosmos DB keeps it, in its own partition, so joining reads it directly.</summary>
/// <param name="Id">The document id, which is the code.</param>
/// <param name="Code">The code, which is also the partition key.</param>
public sealed record LeagueInvite(string Id, string Code, string LeagueId)
{
    public static LeagueInvite For(string code, string leagueId) => new(code, code, leagueId);
}
