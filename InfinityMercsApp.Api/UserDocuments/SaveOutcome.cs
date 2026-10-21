using InfinityMercsApp.Infrastructure.API.UserDocuments;

namespace InfinityMercsApp.Api.UserDocuments;

/// <summary>How a save went, and the file as it now is: the saved one, or the current one after a conflict.</summary>
public sealed record SaveOutcome<TContent>(SaveResult Result, UserDocumentView<TContent>? Current)
    where TContent : class
{
    public static SaveOutcome<TContent> NotFound { get; } = new(SaveResult.NotFound, null);
}
