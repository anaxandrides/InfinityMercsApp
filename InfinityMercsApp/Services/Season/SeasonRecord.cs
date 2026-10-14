namespace InfinityMercsApp.Services.Season;

/// <summary>A saved season as listed for loading.</summary>
public sealed record SeasonRecord(string SeasonFilePath, string CompanyFilePath, string CompanyName, DateTimeOffset StartedUtc)
{
    public string Subtitle => $"Started {StartedUtc:yyyy-MM-dd}";
}
