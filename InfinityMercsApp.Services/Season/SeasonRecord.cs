namespace InfinityMercsApp.Services.Season;

/// <summary>A saved season as listed for loading.</summary>
public sealed record SeasonRecord(string SeasonFilePath, string CompanyFilePath, string CompanyName, DateTimeOffset StartedUtc)
{
    /// <summary>The season's own id, which names it online.</summary>
    public string SeasonIdentifier { get; init; } = string.Empty;

    public string Subtitle => $"Started {StartedUtc:yyyy-MM-dd}";
}
