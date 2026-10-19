using InfinityMercsApp.Domain.Season.Rolls;
using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.Domain.Season.Injuries;

/// <summary>Rules for a trooper taken out during a game: avoiding injury and which injury it takes.</summary>
public static class InjuryRules
{
    public static IReadOnlyList<InjuryTableEntry> Table { get; } =
    [
        new(1, 6, Injury.BattleFury),
        new(7, 8, Injury.PuncturedLung),
        new(9, 10, Injury.Arms),
        new(11, 12, Injury.BrainInjury),
        new(13, 14, Injury.LegsInjury),
        new(15, 16, Injury.BodyInjury),
        new(17, 18, Injury.EyesInjury),
        new(19, 20, Injury.ShellShocked),
    ];

    /// <summary>The d20 roll needed to avoid injury (PH - 3), or null when PH is not a number.</summary>
    public static int? AvoidTarget(string? ph) => int.TryParse(ph, out var value) ? value - 3 : null;

    /// <summary>The table row for a d20 roll, or -1 when the roll is off the table.</summary>
    public static int TableIndexForRoll(int roll) => Table.ToList().FindIndex(e => roll >= e.Min && roll <= e.Max);

    /// <summary>The table row for an injury.</summary>
    public static InjuryTableEntry EntryFor(Injury injury) => Table.First(entry => entry.Injury == injury);

    /// <summary>
    /// An injury rolled on the table for a trooper that already has <paramref name="current"/>: each injury is
    /// unique, so one it has is rolled again. Null when it already has them all.
    /// </summary>
    public static Injury? RollNew(IDice dice, IReadOnlyCollection<Injury> current)
    {
        if (Table.All(entry => current.Contains(entry.Injury)))
        {
            return null;
        }

        while (true)
        {
            var index = TableIndexForRoll(dice.RollD20());
            if (index >= 0 && !current.Contains(Table[index].Injury))
            {
                return Table[index].Injury;
            }
        }
    }

    /// <summary>
    /// Who sheds an injury at the end of a contract: every injured trooper that was not deployed, and the captain
    /// for an injury taken before this contract even when deployed.
    /// </summary>
    public static bool Recovers(bool wasDeployed, bool isCaptain, bool hasInjuryFromBefore) =>
        hasInjuryFromBefore && (!wasDeployed || isCaptain);

    /// <summary>
    /// Who the Captain's injury-healing can go to: the Captain, or, on an elite deployment, any trooper deployed
    /// with it, for an injury taken before this contract. Troopers that rested recover on their own.
    /// </summary>
    public static bool CanTakeCaptainsHealing(bool isCaptain, bool wasDeployed, bool isEliteDeployment, bool hasInjuryFromBefore) =>
        hasInjuryFromBefore && (isCaptain || (isEliteDeployment && wasDeployed));
}
