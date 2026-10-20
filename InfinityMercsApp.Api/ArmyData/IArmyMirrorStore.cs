using InfinityMercsApp.Infrastructure.API.ArmyMirror;

namespace InfinityMercsApp.Api.ArmyData;

/// <summary>Where the army data mirror keeps its manifest and the payloads it copied.</summary>
public interface IArmyMirrorStore
{
    /// <summary>The manifest, or null before the first copy.</summary>
    Task<ArmyMirrorManifest?> ReadManifestAsync(CancellationToken cancellationToken = default);

    Task WriteManifestAsync(ArmyMirrorManifest manifest, CancellationToken cancellationToken = default);

    /// <summary>Keeps <paramref name="json"/> under <paramref name="name"/>, replacing what was there.</summary>
    Task WritePayloadAsync(string name, string json, string hash, CancellationToken cancellationToken = default);

    /// <summary>The payload kept under <paramref name="name"/>, gzipped, or null when there is none.</summary>
    Task<StoredPayload?> OpenPayloadAsync(string name, CancellationToken cancellationToken = default);
}
