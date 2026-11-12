namespace InfinityMercsApp.Api.Leagues.Changes;

/// <summary>What a change decided: the writes that make it and the league season it touched, or why it can't be made.</summary>
/// <param name="Season">The league season to show once the change is made.</param>
public sealed record LeagueDecision(LeagueChangeStatus Status, LeagueWrites Writes, int Season, IReadOnlyList<string> Problems)
{
    public static LeagueDecision Done(LeagueWrites writes, int season) => new(LeagueChangeStatus.Done, writes, season, []);

    public static LeagueDecision NotFound { get; } = Refused(LeagueChangeStatus.NotFound, []);

    public static LeagueDecision Forbidden { get; } = Refused(LeagueChangeStatus.Forbidden, []);

    public static LeagueDecision Rejected(string problem) => Refused(LeagueChangeStatus.Rejected, [problem]);

    /// <summary>A change to the league document alone, written on the ETag it was read at.</summary>
    public static LeagueDecision For(LeagueChangeResult result, LeagueReads reads) => result switch
    {
        { Status: LeagueChangeStatus.Done, League: { } league } when league == reads.League => Done(LeagueWrites.None, league.CurrentSeason),
        { Status: LeagueChangeStatus.Done, League: { } league } => Done(LeagueWrites.Of(DocumentWrites.Replace(league, reads.ETag)), league.CurrentSeason),
        _ => Refused(result.Status, result.Problems)
    };

    private static LeagueDecision Refused(LeagueChangeStatus status, IReadOnlyList<string> problems) => new(status, LeagueWrites.None, 0, problems);
}
