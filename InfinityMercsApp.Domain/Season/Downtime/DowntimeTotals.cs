namespace InfinityMercsApp.Domain.Season.Downtime;

/// <summary>What a resolved downtime choice adds up to, as recorded on the season round.</summary>
public sealed record DowntimeTotals(
    int CrGain,
    int NotorietyGain,
    int XpGain,
    int SpentCr,
    double SwcGain,
    IReadOnlyList<string> OtherEffects);
