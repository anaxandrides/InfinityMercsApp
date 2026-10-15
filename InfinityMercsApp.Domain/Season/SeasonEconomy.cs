using System.Globalization;
using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Domain.Season;

/// <summary>
/// How a mercenary company earns and spends CR, and builds up SWC, across a season: mission rewards,
/// downtime gains and marketplace transactions.
/// </summary>
public static class SeasonEconomy
{
    /// <summary>SWC earned for every mission played.</summary>
    public const double SwcPerMission = 0.5;

    public const int CreditsPerObjective = 4;

    public const int VictoryCredits = 10;

    private const int MaxMissionCredits = 40;

    /// <summary>Credits for playing a round: 10 per round number (from 1), at most 40.</summary>
    public static int MissionCredits(int roundIndex) => Math.Min(10 * Math.Max(1, roundIndex), MaxMissionCredits);

    public static int ObjectiveCredits(int objectivesScored) => CreditsPerObjective * objectivesScored;

    public static int VictoryBonus(bool won) => won ? VictoryCredits : 0;

    /// <summary>
    /// Everything earned and spent over the season: the starting funds, what was bought before the first
    /// contract, then each round's rewards, downtime and purchases.
    /// </summary>
    public static SeasonLedger Ledger(SeasonFile? seasonFile)
    {
        if (seasonFile is null)
        {
            return default;
        }

        int crEarned = 0, crSpent = 0;
        double swcEarned = 0, swcBought = 0;
        void Record(SeasonMarketplace marketplace)
        {
            foreach (var tx in marketplace.Transactions)
            {
                crSpent += tx.CostCr;
                swcBought += (double)(tx.SwcGrant ?? 0);
            }
        }

        Record(seasonFile.InitialPurchases);
        foreach (var round in seasonFile.Rounds)
        {
            crEarned += MissionCredits(round.RoundIndex)
                        + ObjectiveCredits(round.MissionResults.OpScored)
                        + VictoryBonus(round.MissionResults.Won)
                        + round.Downtime.CrGain;
            crSpent += round.Downtime.SpentCr;

            swcEarned += SwcPerMission;
            swcBought += round.Downtime.SwcGain;
            Record(round.Marketplace);
        }

        var start = seasonFile.StartingFunds;
        return new SeasonLedger(start?.Credits ?? 0, crEarned, crSpent, start?.Swc ?? 0, swcEarned, swcBought);
    }

    /// <summary>
    /// The SWC an exchange item grants, read from its name ("SWC +1", "+0.5 SWC"); null when it names none.
    /// </summary>
    public static decimal? SwcGrantFromItemName(string itemName)
    {
        var match = Regex.Match(itemName, @"SWC\s*\+\s*(\d+\.?\d*)|^\+\s*(\d+\.?\d*)\s*SWC", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return null;
        }

        var raw = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    /// <summary>A balance for the title bar: whole numbers plain, otherwise one decimal ("2", "1.5").</summary>
    public static string BalanceText(int credits, double swc)
    {
        var swcText = swc % 1 == 0
            ? ((int)swc).ToString(CultureInfo.InvariantCulture)
            : swc.ToString("0.0", CultureInfo.InvariantCulture);
        return $"{credits} CR - {swcText} SWC";
    }

    /// <summary>An item price: "12cr", or "12cr / 0.5swc" when it also costs SWC.</summary>
    public static string PriceText(int credits, decimal? swc) =>
        swc.HasValue ? $"{credits}cr / {SwcText(swc.Value)}swc" : $"{credits}cr";

    /// <summary>An SWC amount with up to two decimals ("1", "0.5", "1.25").</summary>
    public static string SwcText(decimal swc) => swc.ToString("0.##", CultureInfo.InvariantCulture);
}
