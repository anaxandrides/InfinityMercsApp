namespace InfinityMercsApp.Web.Files;

/// <summary>How fetching the user's files went.</summary>
public enum LoadStatus
{
    Loaded,

    /// <summary>The sign-in has to be renewed before the API can be called.</summary>
    SignInNeeded,

    NotFound,

    /// <summary>The API couldn't be reached or failed.</summary>
    Unavailable
}
