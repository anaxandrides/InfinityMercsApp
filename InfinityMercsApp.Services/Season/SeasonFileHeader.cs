namespace InfinityMercsApp.Services.Season;

/// <summary>The parts of a season file the season list reads.</summary>
internal sealed class SeasonFileHeader
{
    public string SeasonIdentifier { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyIdentifier { get; init; } = string.Empty;
    public string CompanyFilePath { get; init; } = string.Empty;
    public string CreatedDate { get; init; } = string.Empty;
}
