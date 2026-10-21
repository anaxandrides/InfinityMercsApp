using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Domain.Season.Retirement;

/// <summary>
/// Retiring a trooper between contracts: its gear goes back to the company inventory, it can't be used in later
/// contracts, and its renown without that gear is kept for the company. The captain can never retire.
/// </summary>
public static class TrooperRetirement
{
    /// <summary>
    /// Records the retirement in <paramref name="season"/>: the trooper and its peripherals are marked retired and
    /// their gear is taken off, which leaves it in the inventory. Only the trooper carries renown.
    /// </summary>
    public static void Apply(SeasonFile season, RetiringTrooper trooper)
    {
        var afterRound = season.Rounds.Count;
        season.Retired.Add(new RetiredTrooper { EntryIndex = trooper.EntryIndex, Name = trooper.Name, Renown = trooper.Renown, AfterRound = afterRound });
        season.UnitGear.Remove(trooper.EntryIndex);
        foreach (var peripheral in trooper.Peripherals)
        {
            season.Retired.Add(new RetiredTrooper { EntryIndex = peripheral.EntryIndex, Name = peripheral.Name, AfterRound = afterRound });
            season.UnitGear.Remove(peripheral.EntryIndex);
        }
    }
}
