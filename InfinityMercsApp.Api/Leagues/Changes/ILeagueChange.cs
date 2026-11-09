namespace InfinityMercsApp.Api.Leagues.Changes;

/// <summary>
/// One change a user makes to a league. It is worked out from the league as read, and worked out again from a fresh
/// read when someone else changed the league in the meantime.
/// </summary>
public interface ILeagueChange
{
    /// <summary>The league with the change made, or why it can't be.</summary>
    LeagueChangeResult Apply(LeagueDocument league, string userId);
}
