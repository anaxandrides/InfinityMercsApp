using InfinityMercsApp.ApiClient.UserDocuments;

namespace InfinityMercsApp.Api.UserDocuments;

/// <summary>The ids users' files are kept under.</summary>
public static class UserDocumentIds
{
    /// <summary>The Cosmos DB id of a file: its kind and its own id, since a company and a season may share an id.</summary>
    public static string For(UserDocumentKind kind, string documentId) => $"{kind.ToString().ToLowerInvariant()}-{documentId}";
}
