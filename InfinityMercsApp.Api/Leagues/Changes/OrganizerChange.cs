namespace InfinityMercsApp.Api.Leagues.Changes;

/// <summary>A change only the league's organizer may make; other members are refused.</summary>
public abstract class OrganizerChange : ILeagueChange
{
    /// <inheritdoc/>
    public LeagueChangeResult Apply(LeagueDocument league, string userId)
    {
        if (league.MemberFor(userId) is null)
        {
            return LeagueChangeResult.NotFound;
        }

        return league.IsOrganizer(userId) ? ApplyAsOrganizer(league) : LeagueChangeResult.Forbidden;
    }

    /// <summary>The league with the change made, the user being its organizer.</summary>
    protected abstract LeagueChangeResult ApplyAsOrganizer(LeagueDocument league);
}
