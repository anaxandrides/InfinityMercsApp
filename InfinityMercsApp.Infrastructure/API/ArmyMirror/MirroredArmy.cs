namespace InfinityMercsApp.Infrastructure.API.ArmyMirror;

/// <summary>An army the mirror holds.</summary>
/// <param name="Version">The army's own version, as in its payload.</param>
/// <param name="Hash">The SHA-256 of the payload as Corvus Belli served it, in lower-case hex; also its ETag.</param>
/// <param name="UpdatedAt">When this payload was copied.</param>
public sealed record MirroredArmy(int FactionId, string Version, string Hash, DateTimeOffset UpdatedAt);
