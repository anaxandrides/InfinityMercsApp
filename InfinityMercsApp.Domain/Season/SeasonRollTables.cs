namespace InfinityMercsApp.Domain.Season;

/// <summary>The season's d20 tables.</summary>
public static class SeasonRollTables
{
    /// <summary>The pistol a downtime "Weapon" result awards.</summary>
    public static RollTable Pistols { get; } = new(
    [
        new(1, 4, "MULTI Pistol"),
        new(5, 8, "AP Heavy Pistol"),
        new(9, 12, "Breaker Pistol"),
        new(13, 16, "Silenced Pistol"),
        new(17, 20, "Viral Pistol")
    ]);

    /// <summary>The market found when searching for a temporary store; 19-20 finds none.</summary>
    public static RollTable TemporaryStores { get; } = new(
    [
        new(1, 2, "Number One"),
        new(3, 4, "Jade Temu"),
        new(5, 6, "Arachne Req"),
        new(7, 8, "Salaam Suuk"),
        new(9, 10, "Frontier General"),
        new(11, 12, "Alpha Sec"),
        new(13, 14, "Greengrocer"),
        new(15, 16, "Bantai Yamaco"),
        new(17, 18, "Exrah Surplus")
    ]);
}
