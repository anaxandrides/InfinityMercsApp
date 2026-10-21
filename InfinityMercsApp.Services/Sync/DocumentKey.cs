using InfinityMercsApp.Infrastructure.API.UserDocuments;

namespace InfinityMercsApp.Services.Sync;

/// <summary>Names one of the user's files: a company and a season may share an id.</summary>
public sealed record DocumentKey(UserDocumentKind Kind, string DocumentId);
