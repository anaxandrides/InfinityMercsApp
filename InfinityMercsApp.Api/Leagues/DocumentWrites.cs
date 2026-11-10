namespace InfinityMercsApp.Api.Leagues;

/// <summary>Makes <see cref="DocumentWrite{T}"/>s.</summary>
public static class DocumentWrites
{
    public static DocumentWrite<T> Create<T>(T document)
        where T : ILeagueItem => new(DocumentWriteKind.Create, document, null);

    public static DocumentWrite<T> Replace<T>(T document, string etag)
        where T : ILeagueItem => new(DocumentWriteKind.Replace, document, etag);

    public static DocumentWrite<T> Delete<T>(T document, string etag)
        where T : ILeagueItem => new(DocumentWriteKind.Delete, document, etag);
}
