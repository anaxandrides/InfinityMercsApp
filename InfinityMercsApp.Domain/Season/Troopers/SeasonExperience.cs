using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Domain.Season.Troopers;

/// <summary>The XP troopers have earned over a season, from their missions and downtime events.</summary>
public static class SeasonExperience
{
    /// <summary>
    /// The XP the trooper has earned this season. Results saved before troopers were recorded by entry are
    /// matched by <paramref name="name"/> instead.
    /// </summary>
    public static int GainedBy(SeasonFile? seasonFile, int entryIndex, string name)
    {
        var total = 0;
        foreach (var round in seasonFile?.Rounds ?? [])
        {
            total += round.MissionResults.UnitResults
                .Where(result => Matches(result.EntryIndex, result.UnitName, entryIndex, name))
                .Sum(result => result.XpGained);

            if (round.Downtime.XpGain != 0 &&
                Matches(round.Downtime.ParticipantEntryIndex, round.Downtime.ParticipantName, entryIndex, name))
            {
                total += round.Downtime.XpGain;
            }
        }

        return total;
    }

    private static bool Matches(int? recordedIndex, string recordedName, int entryIndex, string name) =>
        recordedIndex is { } index
            ? index == entryIndex
            : !string.IsNullOrWhiteSpace(recordedName) && string.Equals(recordedName.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase);
}
