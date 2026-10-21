using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Domain.Season;

/// <summary>Where a season has got to.</summary>
public static class SeasonRounds
{
    /// <summary>The round being played: the highest round index, or the number of rounds when none is indexed; 0 before any.</summary>
    public static int Current(SeasonFile? seasonFile)
    {
        var rounds = seasonFile?.Rounds ?? [];
        if (rounds.Count == 0)
        {
            return 0;
        }

        var highestRoundIndex = rounds.Max(round => Math.Max(0, round.RoundIndex));
        return highestRoundIndex > 0 ? highestRoundIndex : rounds.Count;
    }

    /// <summary>The round with the highest index, or null before any.</summary>
    public static SeasonRound? Latest(SeasonFile? seasonFile) =>
        seasonFile?.Rounds.OrderByDescending(round => round.RoundIndex).FirstOrDefault();

    /// <summary>
    /// Where purchases are recorded now: the latest round's marketplace (shopping in its downtime), or the
    /// season's initial purchases before the first contract.
    /// </summary>
    public static SeasonMarketplace CurrentMarketplace(SeasonFile seasonFile) =>
        Latest(seasonFile)?.Marketplace ?? seasonFile.InitialPurchases;

    /// <summary>The company's notoriety: what every round's downtime has added.</summary>
    public static int CompanyNotoriety(SeasonFile? seasonFile) =>
        seasonFile?.Rounds.Sum(round => round.Downtime.NotorietyGain) ?? 0;
}
