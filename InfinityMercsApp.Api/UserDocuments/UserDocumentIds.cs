using InfinityMercsApp.Infrastructure.API.UserDocuments;

namespace InfinityMercsApp.Api.UserDocuments;

/// <summary>The ids users' files are kept under.</summary>
public static class UserDocumentIds
{
    private const int MaxIdLength = 64;

    /// <summary>The Cosmos DB id of a file: its kind and its own id, since a company and a season may share an id.</summary>
    public static string For(UserDocumentKind kind, string documentId) => $"{kind.ToString().ToLowerInvariant()}-{documentId}";

    /// <summary>Whether <paramref name="documentId"/> can name a file: 1 to 64 letters, digits, '-' or '_', as a GUID is.</summary>
    public static bool IsValid(string documentId) =>
        documentId.Length is > 0 and <= MaxIdLength && documentId.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
