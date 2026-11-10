using InfinityMercsApp.Api.Leagues.Changes;
using InfinityMercsApp.Domain.Models.Leagues;

namespace InfinityMercsApp.Api.Leagues;

/// <summary>How a request to change a league went: the league as the user now sees it, or why it wasn't changed.</summary>
public sealed record LeagueOutcome(LeagueChangeStatus Status, LeagueView? View, IReadOnlyList<string> Problems)
{
    public static LeagueOutcome Done(LeagueView view) => new(LeagueChangeStatus.Done, view, []);

    public static LeagueOutcome NotFound { get; } = new(LeagueChangeStatus.NotFound, null, []);

    public static LeagueOutcome Forbidden { get; } = new(LeagueChangeStatus.Forbidden, null, []);

    public static LeagueOutcome Conflict { get; } = new(LeagueChangeStatus.Conflict, null, []);

    public static LeagueOutcome Rejected(string problem) => new(LeagueChangeStatus.Rejected, null, [problem]);

    /// <summary>A change that wasn't made, for the reason it gave.</summary>
    public static LeagueOutcome Refused(LeagueDecision decision) => new(decision.Status, null, decision.Problems);
}
