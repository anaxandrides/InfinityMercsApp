namespace InfinityMercsApp.Services.Season;

/// <summary>A trooper taken out during the game.</summary>
public sealed class PendingInjury
{
    public int EntryIndex { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PhValue { get; set; } = "-";
    public string? CachedLogoPath { get; set; }
    public string? PackagedLogoPath { get; set; }
}
