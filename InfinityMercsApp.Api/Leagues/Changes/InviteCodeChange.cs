namespace InfinityMercsApp.Api.Leagues.Changes;

/// <summary>The organizer gives the league a new invite code, or closes joining with none.</summary>
public sealed class InviteCodeChange(string? code) : OrganizerChange
{
    /// <inheritdoc/>
    protected override LeagueChangeResult ApplyAsOrganizer(LeagueDocument league) =>
        LeagueChangeResult.Done(league with { InviteCode = code });
}
