namespace InfinityMercsApp.Api.ArmyData;

/// <summary>A payload as the mirror keeps it.</summary>
/// <param name="GzippedContent">The JSON, gzipped; the reader disposes it.</param>
/// <param name="Hash">The SHA-256 of the JSON, which is also its ETag.</param>
public sealed record StoredPayload(Stream GzippedContent, string Hash);
