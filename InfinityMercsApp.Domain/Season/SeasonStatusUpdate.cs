using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Domain.Season;

/// <summary>Brings a season file's running status up to date with its rounds.</summary>
public static class SeasonStatusUpdate
{
    /// <summary>Recomputes the CR and SWC totals, and each trooper's experience and notoriety, from the rounds played.</summary>
    public static void Apply(SeasonFile seasonFile)
    {
        var status = seasonFile.CurrentStatus;
        var unitXp = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var unitNotoriety = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var round in seasonFile.Rounds)
        {
            foreach (var result in round.MissionResults.UnitResults)
            {
                unitXp[result.UnitName] = unitXp.GetValueOrDefault(result.UnitName, 0) + result.XpGained;
            }

            if (round.Downtime.NotorietyGain != 0 && !string.IsNullOrWhiteSpace(round.Downtime.ParticipantName))
            {
                var name = round.Downtime.ParticipantName;
                unitNotoriety[name] = unitNotoriety.GetValueOrDefault(name, 0) + round.Downtime.NotorietyGain;
            }
        }

        var ledger = SeasonEconomy.Ledger(seasonFile);
        status.CrEarned = ledger.CrEarned;
        status.CrSpent = ledger.CrSpent;
        status.SwcEarned = ledger.SwcEarned;
        status.SwcBought = ledger.SwcBought;
        status.SwcSpent = ledger.SwcSpent;

        foreach (var (name, xp) in unitXp)
        {
            UnitStatus(status, name).TotalExperience = xp;
        }

        foreach (var (name, notoriety) in unitNotoriety)
        {
            UnitStatus(status, name).Notoriety = notoriety;
        }
    }

    private static SeasonUnitStatus UnitStatus(SeasonStatus status, string name)
    {
        var unit = status.Units.FirstOrDefault(candidate => string.Equals(candidate.UnitName, name, StringComparison.OrdinalIgnoreCase));
        if (unit is null)
        {
            unit = new SeasonUnitStatus { UnitName = name };
            status.Units.Add(unit);
        }

        return unit;
    }
}
