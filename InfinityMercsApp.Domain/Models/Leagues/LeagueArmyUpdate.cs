namespace InfinityMercsApp.Domain.Models.Leagues;

/// <summary>The organizer moving the league to other army data.</summary>
public class LeagueArmyUpdate
{
    /// <summary>The army data snapshot the league now plays with; null for whatever is current.</summary>
    public string? ArmySnapshotId { get; set; }
}
