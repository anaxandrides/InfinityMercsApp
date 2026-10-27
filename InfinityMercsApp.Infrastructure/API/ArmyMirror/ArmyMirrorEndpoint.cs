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

    /// <summary>A generated company's faction (Airborne, Inspiring, TAG), already built (ArmyDefinitionJson).</summary>
    public Uri Generated(int factionId) => new(BaseAddress, $"army/generated/{factionId}");

    /// <summary>Which factions the Airborne, Inspiring and Cohesive companies list, worked out by the API.</summary>
    public Uri Listings => new(BaseAddress, "army/listings");

    /// <summary>Every faction's and unit's logo in one zip.</summary>
    public Uri Logos => new(BaseAddress, "army/logos");
}
