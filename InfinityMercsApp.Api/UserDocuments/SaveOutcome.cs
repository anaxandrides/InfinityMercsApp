using InfinityMercsApp.Infrastructure.API.UserDocuments;

namespace InfinityMercsApp.Api.UserDocuments;

/// <summary>How a save went, and the file as it now is: the saved one, or the current one after a conflict.</summary>
public sealed record SaveOutcome(SaveResult Result, UserDocumentView? Current)
{
    public static SaveOutcome NotFound { get; } = new(SaveResult.NotFound, null);
}
