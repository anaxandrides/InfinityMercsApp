namespace InfinityMercsApp.Api.UserDocuments;

/// <summary>How a save of a user's file went.</summary>
public enum SaveResult
{
    Saved,

    /// <summary>The file changed since the client read it, or it already exists; the client merges with the current one.</summary>
    Conflict,

    /// <summary>There is no such file to delete.</summary>
    NotFound
}
