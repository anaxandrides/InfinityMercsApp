namespace InfinityMercsApp.Api.Leagues.Changes;

/// <summary>
/// The organizer starts the league's next season. Members then enter new company seasons; earlier ones keep their
/// standings.
/// </summary>
/// <param name="fromSeason">The season the organizer saw as current, so starting twice by mistake starts one season.</param>
public sealed class NextSeasonChange(int fromSeason) : OrganizerChange
{
    /// <inheritdoc/>
    protected override LeagueChangeResult ApplyAsOrganizer(LeagueDocument league) =>
        league.CurrentSeason == fromSeason
            ? LeagueChangeResult.Done(league with { CurrentSeason = league.CurrentSeason + 1 })
            : LeagueChangeResult.Done(league);
}
