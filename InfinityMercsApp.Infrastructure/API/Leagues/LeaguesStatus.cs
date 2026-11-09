namespace InfinityMercsApp.Infrastructure.API.Leagues;

/// <summary>How the API answered a league request.</summary>
public enum LeaguesStatus
{
    Ok,

    /// <summary>No such league, invite, entry or game; or the user doesn't belong to the league.</summary>
    NotFound,

    /// <summary>The sign-in token was missing or not accepted.</summary>
    Unauthorized,

    /// <summary>The user belongs to the league but may not do that; most changes are the organizer's.</summary>
    Forbidden,

    /// <summary>The request breaks a league rule; the reply says which.</summary>
    Rejected,

    /// <summary>The league kept changing under the request; worth trying again.</summary>
    Conflict,

    /// <summary>The API couldn't be reached or failed; worth trying again later.</summary>
    Unavailable
}
