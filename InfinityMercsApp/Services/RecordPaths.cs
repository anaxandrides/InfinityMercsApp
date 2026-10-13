namespace InfinityMercsApp.Services;

/// <summary>Where saved companies and seasons live.</summary>
/// <param name="CompaniesDirectory">The MercenaryRecords folder holding saved company files.</param>
public sealed record RecordPaths(string CompaniesDirectory)
{
    public string SeasonsDirectory => Path.Combine(CompaniesDirectory, "Seasons");

    public static RecordPaths ForAppData(string appDataDirectory) => new(Path.Combine(appDataDirectory, "MercenaryRecords"));
}
