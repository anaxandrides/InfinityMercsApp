using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.UnitTests.Season.Troopers;

public sealed class SeasonExperienceTests
{
    private static SeasonRound Round(IEnumerable<SeasonMissionUnitResult> results, SeasonDowntimeEntry? downtime = null) => new()
    {
        MissionResults = new SeasonMissionResult { UnitResults = results.ToList() },
        Downtime = downtime ?? new SeasonDowntimeEntry()
    };

    [Fact]
    public void AddsMissionAndDowntimeXpByEntry()
    {
        var season = new SeasonFile
        {
            Rounds =
            [
                Round([new() { EntryIndex = 1, UnitName = "Bolt", XpGained = 5 }, new() { EntryIndex = 2, UnitName = "Bolt", XpGained = 9 }]),
                Round([new() { EntryIndex = 1, UnitName = "Sparky", XpGained = 4 }], new SeasonDowntimeEntry { ParticipantEntryIndex = 1, ParticipantName = "Sparky", XpGain = 3 })
            ]
        };

        Assert.Equal(12, SeasonExperience.GainedBy(season, 1, "Sparky"));
        Assert.Equal(9, SeasonExperience.GainedBy(season, 2, "Bolt"));
    }

    [Fact]
    public void OlderResultsMatchByName()
    {
        var season = new SeasonFile
        {
            Rounds = [Round([new() { UnitName = "Bolt", XpGained = 5 }], new SeasonDowntimeEntry { ParticipantName = "bolt ", XpGain = 2 })]
        };

        Assert.Equal(7, SeasonExperience.GainedBy(season, 3, "Bolt"));
        Assert.Equal(0, SeasonExperience.GainedBy(season, 3, "Zond"));
        Assert.Equal(0, SeasonExperience.GainedBy(null, 3, "Bolt"));
    }
}
