using InfinityMercsApp.Infrastructure.API.UserDocuments;

namespace InfinityMercsApp.Api.UserDocuments;

/// <summary>The route segments of the user's files: "companies" and "seasons", and the files' ids.</summary>
public static class UserDocumentRoutes
{
    private const int MaxIdLength = 64;

    /// <summary>The kind a collection segment names, or null for any other segment.</summary>
    public static UserDocumentKind? KindOf(string collection) => collection switch
    {
        "companies" => UserDocumentKind.Company,
        "seasons" => UserDocumentKind.Season,
        _ => null
    };

    /// <summary>Whether <paramref name="documentId"/> can name a file: 1 to 64 letters, digits, '-' or '_', as a GUID is.</summary>
    public static bool IsDocumentId(string documentId) =>
        documentId.Length is > 0 and <= MaxIdLength && documentId.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
