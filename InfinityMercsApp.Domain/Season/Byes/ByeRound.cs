using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Domain.Season.Byes;

/// <summary>
/// A contract the company could not play. It still earns the round's normal rewards and 3 OP worth of CR (see
/// <see cref="SeasonEconomy.ByeCredits"/>); to make up for the lost XP, up to 4 troopers gain 7 XP each or up to 6
/// gain 5 XP each; and one injured trooper has an injury written off.
/// </summary>
public static class ByeRound
{
    public static int XpEach(ByeXpSplit split) => split == ByeXpSplit.FourTroopers ? 7 : 5;

    public static int MostTroopers(ByeXpSplit split) => split == ByeXpSplit.FourTroopers ? 4 : 6;

    /// <summary>True when the choice gives XP to no more troopers than its split allows, each one once.</summary>
    public static bool IsValid(ByeRoundChoice choice) =>
        choice.Troopers.Count <= MostTroopers(choice.Split) &&
        choice.Troopers.Select(trooper => trooper.EntryIndex).Distinct().Count() == choice.Troopers.Count;

    /// <summary>
    /// Adds the bye as the season's next round, with the chosen troopers' XP, and writes off the injury.
    /// </summary>
    /// <returns>The new round.</returns>
    /// <exception cref="ArgumentException">The choice is not <see cref="IsValid"/>.</exception>
    public static SeasonRound Apply(SeasonFile season, ByeRoundChoice choice)
    {
        if (!IsValid(choice))
        {
            throw new ArgumentException($"A bye gives XP to at most {MostTroopers(choice.Split)} different troopers.", nameof(choice));
        }

        var xp = XpEach(choice.Split);
        var round = new SeasonRound
        {
            RoundIndex = SeasonRounds.Current(season) + 1,
            MissionResults = new SeasonMissionResult
            {
                IsBye = true,
                UnitResults = choice.Troopers
                    .Select(trooper => new SeasonMissionUnitResult { EntryIndex = trooper.EntryIndex, UnitName = trooper.Name, XpGained = xp })
                    .ToList()
            }
        };
        season.Rounds.Add(round);

        if (choice.WriteOff is { } writeOff && season.Troopers.TryGetValue(writeOff.EntryIndex, out var trooper))
        {
            trooper.Injuries.Remove(writeOff.Injury);
        }

        return round;
    }
}
