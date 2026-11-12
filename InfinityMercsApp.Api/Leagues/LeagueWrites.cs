namespace InfinityMercsApp.Api.Leagues;

/// <summary>
/// What a change writes to a league's partition, all or none: the league itself, and the entries and games it adds,
/// replaces or removes.
/// </summary>
public sealed record LeagueWrites(
    DocumentWrite<LeagueDocument>? League,
    IReadOnlyList<DocumentWrite<LeagueEntry>> Entries,
    IReadOnlyList<DocumentWrite<LeagueGame>> Games)
{
    public static LeagueWrites None { get; } = new(null, [], []);

    public bool IsEmpty => League is null && Entries.Count == 0 && Games.Count == 0;

    public static LeagueWrites Of(DocumentWrite<LeagueDocument> league) => None with { League = league };

    public static LeagueWrites Of(DocumentWrite<LeagueEntry> entry) => None with { Entries = [entry] };
}
