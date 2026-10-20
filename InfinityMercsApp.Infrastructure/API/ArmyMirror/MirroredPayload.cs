namespace InfinityMercsApp.Infrastructure.API.ArmyMirror;

/// <summary>A payload the mirror holds.</summary>
/// <param name="Hash">The SHA-256 of the payload as Corvus Belli served it, in lower-case hex; also its ETag.</param>
/// <param name="UpdatedAt">When this payload was copied.</param>
public sealed record MirroredPayload(string Hash, DateTimeOffset UpdatedAt);
