using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season;

namespace InfinityMercsApp.UnitTests.Season;

public sealed class SeasonStatusUpdateTests
{
    [Fact]
    public void For_PlayedRounds_TotalsCreditsExperienceAndNotoriety()
    {
        var season = new SeasonFile
        {
            Rounds =
            [
                new SeasonRound
                {
                    RoundIndex = 1,
                    MissionResults = new SeasonMissionResult { UnitResults = [new SeasonMissionUnitResult { UnitName = "Bolt", XpGained = 3 }] },
                    Downtime = new SeasonDowntimeEntry { ParticipantName = "bolt", NotorietyGain = 1 }
                },
                new SeasonRound
                {
                    RoundIndex = 2,
                    MissionResults = new SeasonMissionResult { UnitResults = [new SeasonMissionUnitResult { UnitName = "BOLT", XpGained = 2 }] },
                    Marketplace = new SeasonMarketplace { Transactions = [new SeasonTransaction { ItemName = "Pistol", CostCr = 3 }] }
                }
            ]
        };

        SeasonStatusUpdate.Apply(season);

        var status = season.CurrentStatus;
        Assert.Equal(SeasonEconomy.Ledger(season).CrEarned, status.CrEarned);
        Assert.Equal(3, status.CrSpent);
        var bolt = Assert.Single(status.Units);
        Assert.Equal((5, 1), (bolt.TotalExperience, bolt.Notoriety));
    }
}
