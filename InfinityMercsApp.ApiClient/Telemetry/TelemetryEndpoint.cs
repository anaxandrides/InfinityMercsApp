namespace InfinityMercsApp.ApiClient.Telemetry;

/// <summary>Where the app reports its telemetry.</summary>
/// <param name="BaseAddress">The API's root, ending in a slash, e.g. https://example.azurewebsites.net/api/.</param>
public sealed record TelemetryEndpoint(Uri BaseAddress)
{
    /// <summary>The API that infra/main.bicep deploys.</summary>
    public static TelemetryEndpoint Production { get; } = new(ApiAddress.Production);

    /// <summary>Takes a <see cref="TelemetryBatch"/> by POST.</summary>
    public Uri Batches => new(BaseAddress, "telemetry");
}
