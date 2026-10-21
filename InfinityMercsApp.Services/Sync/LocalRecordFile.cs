using InfinityMercsApp.Infrastructure.API.UserDocuments;

namespace InfinityMercsApp.Services.Sync;

/// <summary>A company or season file on this device, and the id it is kept under online.</summary>
internal sealed record LocalRecordFile(UserDocumentKind Kind, string DocumentId, string FilePath);
