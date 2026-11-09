namespace InfinityMercsApp.Infrastructure.API.Leagues;

/// <summary>The API's answer to a league request: how it went, what it sent back, and what was wrong with a rejected request.</summary>
public sealed record LeaguesReply<TValue>(LeaguesStatus Status, TValue? Value, IReadOnlyList<string> Problems)
    where TValue : class
{
    public static LeaguesReply<TValue> Of(LeaguesStatus status) => new(status, null, []);
}
