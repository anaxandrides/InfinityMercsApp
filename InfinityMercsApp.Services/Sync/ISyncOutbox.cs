using InfinityMercsApp.ApiClient.UserDocuments;

namespace InfinityMercsApp.Services.Sync;

/// <summary>
/// Where saves and deletions of companies and seasons are noted, so they reach the user's account online when the
/// app is next signed in and connected.
/// </summary>
public interface ISyncOutbox
{
    /// <summary>Notes that the file at <paramref name="filePath"/> was saved.</summary>
    void RecordSave(UserDocumentKind kind, string documentId, string filePath);

    /// <summary>Notes that the file was deleted.</summary>
    void RecordDelete(UserDocumentKind kind, string documentId);
}
