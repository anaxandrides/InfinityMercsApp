namespace InfinityMercsApp.Infrastructure.API.UserDocuments;

/// <summary>The API's answer: how it went, and what it sent back.</summary>
/// <param name="Value">The file or listing on <see cref="UserDocumentsStatus.Ok"/>; the current file on a conflict.</param>
public sealed record UserDocumentsReply<TValue>(UserDocumentsStatus Status, TValue? Value)
    where TValue : class
{
    public static UserDocumentsReply<TValue> Of(UserDocumentsStatus status) => new(status, null);
}
