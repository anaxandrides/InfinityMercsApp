using InfinityMercsApp.ApiClient;

namespace InfinityMercsApp.Infrastructure.API.ArmyMirror;

/// <summary>Where the app's own copy of the army data is served.</summary>
/// <param name="BaseAddress">The API's root, ending in a slash, e.g. https://example.azurewebsites.net/api/.</param>
public sealed record ArmyMirrorEndpoint(Uri BaseAddress)
{
    /// <summary>The army data API that infra/main.bicep deploys.</summary>
    public static ArmyMirrorEndpoint Production { get; } = new(ApiAddress.Production);

    public Uri Manifest => new(BaseAddress, "army/manifest");

    public Uri Metadata => new(BaseAddress, "army/metadata");

    public Uri Army(int factionId) => new(BaseAddress, $"army/units/{factionId}");
}
