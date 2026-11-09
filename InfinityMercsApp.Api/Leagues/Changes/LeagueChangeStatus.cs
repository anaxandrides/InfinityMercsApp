namespace InfinityMercsApp.Api.Leagues.Changes;

/// <summary>How a change to a league went.</summary>
public enum LeagueChangeStatus
{
    Done,

    /// <summary>There is no such league, or the user doesn't belong to it, or it has no such entry or game.</summary>
    NotFound,

    /// <summary>The user belongs to the league but may not make this change.</summary>
    Forbidden,

    /// <summary>The change breaks a league rule; the result says which.</summary>
    Rejected,

    /// <summary>The league kept changing under the change, so it was given up.</summary>
    Conflict
}
