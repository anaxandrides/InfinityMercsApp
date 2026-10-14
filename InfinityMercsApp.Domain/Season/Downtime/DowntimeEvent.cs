namespace InfinityMercsApp.Domain.Season.Downtime;

public sealed class DowntimeEvent
{
    public string RollRange { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<string> EventTraits { get; init; } = [];
    public ParticipantKind RequiredParticipant { get; init; } = ParticipantKind.None;
    public IReadOnlyList<DowntimeChoice> Choices { get; init; } = [];
    public bool IsNoIncident { get; init; }
    // A concrete roll value that maps back to this event bucket (used when the player picks manually).
    public int RepresentativeRoll { get; init; }
}
