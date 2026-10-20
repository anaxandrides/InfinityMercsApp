namespace InfinityMercsApp.Api.ArmyData;

/// <summary>
/// Where the mirror keeps the payloads it copied, each under its hash. A payload is never replaced or removed, so every
/// snapshot that names it can still serve it.
/// </summary>
public interface IArmyPayloadStore
{
    /// <summary>Keeps <paramref name="json"/> under <paramref name="hash"/>, unless a payload is already kept there.</summary>
    Task SaveAsync(string hash, string json, CancellationToken cancellationToken = default);

    /// <summary>The payload kept under <paramref name="hash"/>, gzipped, or null when there is none.</summary>
    Task<StoredPayload?> OpenAsync(string hash, CancellationToken cancellationToken = default);
}
