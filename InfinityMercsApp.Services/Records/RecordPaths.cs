namespace InfinityMercsApp.Services.Records;

/// <summary>Where saved companies and seasons live.</summary>
/// <param name="CompaniesDirectory">The MercenaryRecords folder holding saved company files.</param>
/// <param name="FactionLogoDirectory">The folder of downloaded faction logos, named <c>{factionId}.svg</c>.</param>
public sealed record RecordPaths(string CompaniesDirectory, string FactionLogoDirectory = "")
{
    public string SeasonsDirectory => Path.Combine(CompaniesDirectory, "Seasons");

    /// <summary>What this device knows of the user's files online, and what it has yet to send.</summary>
    public string SyncStatePath => Path.Combine(CompaniesDirectory, "Sync", "sync-state.json");

    public string FactionLogoPath(int factionId) => Path.Combine(FactionLogoDirectory, $"{factionId}.svg");

    public static RecordPaths ForAppData(string appDataDirectory) => new(
        Path.Combine(appDataDirectory, "MercenaryRecords"),
        Path.Combine(appDataDirectory, "svg-cache"));
}
