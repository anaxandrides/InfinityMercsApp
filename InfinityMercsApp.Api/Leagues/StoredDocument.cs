namespace InfinityMercsApp.Api.Leagues;

/// <summary>A document as read, with the ETag a change to it must match.</summary>
public sealed record StoredDocument<T>(T Document, string ETag);
