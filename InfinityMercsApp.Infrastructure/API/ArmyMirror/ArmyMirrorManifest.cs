namespace InfinityMercsApp.Infrastructure.API.ArmyMirror;

/// <summary>
/// A snapshot of the army data mirror: the metadata and every army it copied from Corvus Belli, each with the hash of
/// its payload, so a client downloads only what changed. Each distinct set of payloads is a snapshot that is kept for
/// good, so a company can pin the army data it was built with.
/// </summary>
public sealed record ArmyMirrorManifest
{
    /// <summary>The snapshot's id, worked out from its payloads' hashes: the same army data always has the same id.</summary>
    public string SnapshotId { get; set; } = string.Empty;

    /// <summary>When the mirror last checked Corvus Belli; for a pinned snapshot, when it was taken.</summary>
    public DateTimeOffset CheckedAt { get; init; }

    /// <summary>The metadata payload; null until it was first copied.</summary>
    public MirroredPayload? Metadata { get; init; }

    /// <summary>The armies copied, by faction id.</summary>
    public IReadOnlyList<MirroredArmy> Armies { get; set; } = [];

    /// <summary>
    /// Every faction's and unit's logo in one zip (GET army/logos), named by the hash of the logos in it; null until it
    /// was first built. Not part of the snapshot id: logos don't change what a company was built with.
    /// </summary>
    public MirroredPayload? Logos { get; init; }

    /// <summary>
    /// The Airborne, Inspiring and TAG companies' factions, built from this snapshot's armies so a client needn't build
    /// them itself (GET army/generated/{factionId}); empty until first built. Not part of the snapshot id: they follow
    /// from its armies and <see cref="GeneratedRulesVersion"/>.
    /// </summary>
    public IReadOnlyList<MirroredArmy> Generated { get; set; } = [];

    /// <summary>The generated companies' rules version (GeneratedCompanies.RulesVersion) they were built by; null before.</summary>
    public string? GeneratedRulesVersion { get; init; }

    /// <summary>
    /// Which factions the Airborne, Inspiring and Cohesive companies list (FactionListings), worked out from this
    /// snapshot's armies so a client needn't read every army to list them (GET army/listings); null until first built.
    /// </summary>
    public MirroredPayload? Listings { get; init; }

    /// <summary>The version of how the listings were worked out (FactionListings.Version); null before.</summary>
    public string? ListingsVersion { get; init; }
}
