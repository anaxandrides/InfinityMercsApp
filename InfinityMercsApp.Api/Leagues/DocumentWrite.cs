namespace InfinityMercsApp.Api.Leagues;

/// <summary>One write to a document in a league's partition.</summary>
/// <param name="ETag">The ETag the document was read at, which a replacement or removal must match; null for a creation.</param>
public sealed record DocumentWrite<T>(DocumentWriteKind Kind, T Document, string? ETag)
    where T : ILeagueItem;
