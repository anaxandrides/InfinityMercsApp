using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season;

namespace InfinityMercsApp.UnitTests.Season;

public sealed class SeasonEconomyTests
{
    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 10)]
    [InlineData(3, 30)]
    [InlineData(4, 40)]
    [InlineData(9, 40)]
    public void MissionCredits_Are10PerRoundUpTo40(int round, int expected) =>
        Assert.Equal(expected, SeasonEconomy.MissionCredits(round));

    [Fact]
    public void ObjectiveAndVictoryCredits()
    {
        Assert.Equal(12, SeasonEconomy.ObjectiveCredits(3));
        Assert.Equal(10, SeasonEconomy.VictoryBonus(true));
        Assert.Equal(0, SeasonEconomy.VictoryBonus(false));
    }

    [Fact]
    public void Ledger_TalliesMissionsDowntimeAndMarketplace()
    {
        var season = new SeasonFile
        {
            Rounds =
            [
                new SeasonRound
                {
                    RoundIndex = 1,
                    MissionResults = new SeasonMissionResult { Won = true, OpScored = 2 },
                    Downtime = new SeasonDowntimeEntry { CrGain = 5, SpentCr = 3, SwcGain = 0.5 },
                    Marketplace = new SeasonMarketplace
                    {
                        Transactions =
                        [
                            new SeasonTransaction { CostCr = 7, CostSwc = 1m },
                            new SeasonTransaction { CostCr = 4, SwcGrant = 1.5m }
                        ]
                    }
                },
                new SeasonRound { RoundIndex = 2 }
            ]
        };

        var ledger = SeasonEconomy.Ledger(season);

        // Round 1: 10 mission + 8 objectives + 10 victory + 5 downtime; round 2: 20 mission.
        Assert.Equal(new SeasonLedger(CrEarned: 53, CrSpent: 14, SwcEarned: 1.0, SwcBought: 2.0, SwcSpent: 1.0), ledger);
        Assert.Equal(39, ledger.CreditsBalance);
        Assert.Equal(2.0, ledger.SwcBalance);
    }

    [Fact]
    public void Ledger_OfNoSeasonIsEmpty() => Assert.Equal(default, SeasonEconomy.Ledger(null));

    [Theory]
    [InlineData("SWC +1", "1")]
    [InlineData("swc+0.5 exchange", "0.5")]
    [InlineData("+1.5 SWC", "1.5")]
    [InlineData("Credits for SWC", null)]
    public void SwcGrantFromItemName(string name, string? expected) =>
        Assert.Equal(expected is null ? null : decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture),
            SeasonEconomy.SwcGrantFromItemName(name));

    [Theory]
    [InlineData(12, 2.0, "12 CR - 2 SWC")]
    [InlineData(-3, 1.5, "-3 CR - 1.5 SWC")]
    public void BalanceText(int credits, double swc, string expected) =>
        Assert.Equal(expected, SeasonEconomy.BalanceText(credits, swc));

    [Fact]
    public void PriceText_ShowsSwcOnlyWhenPriced()
    {
        Assert.Equal("12cr", SeasonEconomy.PriceText(12, null));
        Assert.Equal("12cr / 0.5swc", SeasonEconomy.PriceText(12, 0.5m));
        Assert.Equal("3cr / 1swc", SeasonEconomy.PriceText(3, 1.00m));
    }
}
