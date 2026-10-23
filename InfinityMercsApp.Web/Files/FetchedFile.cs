namespace InfinityMercsApp.Web.Files;

/// <summary>One of the user's files, fetched with its content.</summary>
public sealed record FetchedFile<TContent>(string Id, TContent Content)
    where TContent : class;
