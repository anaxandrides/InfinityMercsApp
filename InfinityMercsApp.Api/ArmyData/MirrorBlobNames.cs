namespace InfinityMercsApp.Api.ArmyData;

/// <summary>The names the army data mirror keeps things under.</summary>
public static class MirrorBlobNames
{
    /// <summary>The container, which the Bicep also creates.</summary>
    public const string Container = "army-data";

    public const string Manifest = "manifest.json";

    public const string Metadata = "metadata.json.gz";

    public static string Army(int factionId) => $"armies/{factionId}.json.gz";
}
