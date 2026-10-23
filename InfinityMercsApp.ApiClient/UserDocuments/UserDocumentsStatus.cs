namespace InfinityMercsApp.ApiClient.UserDocuments;

/// <summary>How the API answered a request for one of the user's files.</summary>
public enum UserDocumentsStatus
{
    Ok,

    /// <summary>The file changed on another device since this one read it; the answer holds the current file.</summary>
    Conflict,

    NotFound,

    /// <summary>The sign-in token was missing or not accepted.</summary>
    Unauthorized,

    /// <summary>The API refused the file: it is malformed, fails a check or is too large. Sending it again won't help.</summary>
    Rejected,

    /// <summary>The API couldn't be reached or failed; worth trying again later.</summary>
    Unavailable
}
