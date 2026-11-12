namespace InfinityMercsApp.Api.Leagues.Changes;

/// <summary>A change to the league document alone (its members and settings), worked out from the league as read.</summary>
public abstract class LeagueDocumentChange : ILeagueChange
{
    /// <inheritdoc/>
    public Task<LeagueDecision> DecideAsync(LeagueReads reads, string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(LeagueDecision.For(Apply(reads.League, userId), reads));

    /// <summary>The league with the change made, or why it can't be.</summary>
    public abstract LeagueChangeResult Apply(LeagueDocument league, string userId);
}
