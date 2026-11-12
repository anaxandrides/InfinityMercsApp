using System.Text.Json.Serialization.Metadata;

namespace InfinityMercsApp.ApiClient.UserDocuments;

/// <summary>
/// The JSON contracts of the files the client carries (their views, writes and listings), generated where the file types
/// are known. A file type they don't cover can't be sent or read.
/// </summary>
public sealed record UserDocumentTypes(IJsonTypeInfoResolver Resolver);
