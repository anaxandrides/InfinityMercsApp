namespace InfinityMercsApp.Domain.Models.Leagues;

/// <summary>A league to start; whoever starts it organizes it.</summary>
public class NewLeague
{
    public string Name { get; set; } = string.Empty;

    /// <summary>How the organizer is shown to the league's other members.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>The army data the league plays with; null for whatever is current.</summary>
    public string? ArmySnapshotId { get; set; }
}
