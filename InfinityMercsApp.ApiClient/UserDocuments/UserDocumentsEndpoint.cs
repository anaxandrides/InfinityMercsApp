using System.Globalization;

namespace InfinityMercsApp.ApiClient.UserDocuments;

/// <summary>Where the signed-in user's companies and seasons are kept online.</summary>
/// <param name="BaseAddress">The API's root, ending in a slash, e.g. https://example.azurewebsites.net/api/.</param>
public sealed record UserDocumentsEndpoint(Uri BaseAddress)
{
    /// <summary>The API that infra/main.bicep deploys, which also serves the army data.</summary>
    public static UserDocumentsEndpoint Production { get; } = new(ApiAddress.Production);

    /// <summary>Every file of <paramref name="kind"/>, without their content.</summary>
    public Uri List(UserDocumentKind kind) => new(BaseAddress, Collection(kind));

    public Uri Document(UserDocumentKind kind, string documentId) =>
        new(BaseAddress, $"{Collection(kind)}/{Uri.EscapeDataString(documentId)}");

    /// <summary>Deletes the file, when <paramref name="baseRevision"/> is still its revision.</summary>
    public Uri Delete(UserDocumentKind kind, string documentId, int baseRevision) =>
        new(BaseAddress, $"{Collection(kind)}/{Uri.EscapeDataString(documentId)}?baseRevision={baseRevision.ToString(CultureInfo.InvariantCulture)}");

    private static string Collection(UserDocumentKind kind) => kind switch
    {
        UserDocumentKind.Company => "me/companies",
        UserDocumentKind.Season => "me/seasons",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "No route for this kind of file.")
    };
}
