using InfinityMercsApp.Infrastructure.API.InfinityArmy;

namespace InfinityMercsApp.Infrastructure.API.ArmyMirror;

/// <summary>Which payload to read: the metadata, or one faction's army.</summary>
/// <param name="FactionId">The faction whose army to read; null for the metadata.</param>
public sealed record PayloadRequest(int? FactionId)
{
    public static PayloadRequest Metadata { get; } = new((int?)null);

    public static PayloadRequest Army(int factionId) => new(factionId);

    public Task<string> ReadFromAsync(IArmyPayloadSource source, CancellationToken cancellationToken) =>
        FactionId is { } factionId ? source.GetArmyAsync(factionId, cancellationToken) : source.GetMetadataAsync(cancellationToken);

    public override string ToString() => FactionId is { } factionId ? $"the army of faction {factionId}" : "the metadata";
}
