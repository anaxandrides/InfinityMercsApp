namespace InfinityMercsApp.Api.Leagues.Changes;

/// <summary>The organizer moves the league to other army data.</summary>
public sealed class ArmyChange(string? armySnapshotId) : OrganizerChange
{
    /// <inheritdoc/>
    protected override LeagueChangeResult ApplyAsOrganizer(LeagueDocument league) =>
        LeagueChangeResult.Done(league with { ArmySnapshotId = armySnapshotId });
}
