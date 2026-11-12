namespace InfinityMercsApp.Api.Leagues.Changes;

/// <summary>Joining through an invite, which only works while <paramref name="code"/> is still the league's code.</summary>
public sealed class InviteJoinChange(string code, JoinChange join) : LeagueDocumentChange
{
    /// <inheritdoc/>
    public override LeagueChangeResult Apply(LeagueDocument league, string userId) =>
        league.InviteCode == code ? join.Apply(league, userId) : LeagueChangeResult.NotFound;
}
