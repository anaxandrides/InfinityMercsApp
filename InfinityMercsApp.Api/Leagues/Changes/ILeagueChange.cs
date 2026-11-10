namespace InfinityMercsApp.Api.Leagues.Changes;

/// <summary>
/// One change a user makes to a league. It is decided from the league's documents as read, and decided again from a
/// fresh read when someone else wrote one of them in the meantime.
/// </summary>
public interface ILeagueChange
{
    /// <summary>The writes that make the change, and the league season to show after it; or why it can't be made.</summary>
    Task<LeagueDecision> DecideAsync(LeagueReads reads, string userId, CancellationToken cancellationToken = default);
}
