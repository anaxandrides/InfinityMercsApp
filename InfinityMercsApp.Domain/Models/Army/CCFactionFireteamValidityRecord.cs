namespace InfinityMercsApp.Domain.Models.Army;

public class CCFactionFireteamValidityRecord
{
    public string CacheKey { get; set; } = string.Empty;

    public int FactionId { get; set; }

    public string FilterKey { get; set; } = string.Empty;

    public bool HasValidCoreFireteams { get; set; }

    /// <summary>The CORE fireteam names found valid for this faction filter.</summary>
    public IReadOnlyList<string> ValidCoreFireteams { get; set; } = [];

    public long EvaluatedAtUnixSeconds { get; set; }
}
