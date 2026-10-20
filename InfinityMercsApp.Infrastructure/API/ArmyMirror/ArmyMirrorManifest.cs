namespace InfinityMercsApp.Infrastructure.API.ArmyMirror;

/// <summary>
/// What the army data mirror holds: the metadata and every army it copied from Corvus Belli, each with the hash of its
/// payload, so a client downloads only what changed.
/// </summary>
public sealed record ArmyMirrorManifest
{
    /// <summary>When the mirror last checked Corvus Belli.</summary>
    public DateTimeOffset CheckedAt { get; init; }

    /// <summary>The metadata payload; null until it was first copied.</summary>
    public MirroredPayload? Metadata { get; init; }

    /// <summary>The armies copied, by faction id.</summary>
    public IReadOnlyList<MirroredArmy> Armies { get; init; } = [];
}
