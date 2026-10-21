namespace InfinityMercsApp.Domain.Season.Loot;

/// <summary>
/// Which buffs a loot weapon may gain; an illegal or duplicate one is rerolled. The sheet names Continuous after a
/// non-lethal buff, and Experimental against Plasma. Repeating stacks, so it may come up again. Other combinations
/// are the player's call (the loot box page can reroll the last buff).
/// </summary>
public static class LootBuffRules
{
    public static bool IsLegal(LootBuff candidate, IReadOnlyList<LootBuff> held, LootWeaponRow weapon)
    {
        if (candidate.IsExtra)
        {
            return true;
        }

        if (!candidate.Stacks && held.Any(buff => buff.Roll == candidate.Roll))
        {
            return false;
        }

        var heldNonLethal = weapon.IsNonLethal || held.Any(buff => buff.IsNonLethal);
        if (candidate.Roll == LootBuffChart.ContinuousRoll && heldNonLethal)
        {
            return false;
        }

        if (candidate.IsNonLethal && held.Any(buff => buff.Roll == LootBuffChart.ContinuousRoll))
        {
            return false;
        }

        return candidate.Roll != LootBuffChart.ExperimentalRoll || held.All(buff => buff.Roll != LootBuffChart.PlasmaRoll);
    }

    /// <summary>
    /// Whether <paramref name="dice"/> dice can give the weapon a buff it may take, other than a free roll; when not,
    /// rerolling would never end.
    /// </summary>
    public static bool HasLegal(int dice, IReadOnlyList<LootBuff> held, LootWeaponRow weapon, IReadOnlyCollection<int>? excluded = null) =>
        LootRolls.Results(dice)
            .Where(roll => excluded?.Contains(roll) != true)
            .Select(LootBuffChart.For)
            .Any(buff => !buff.IsExtra && IsLegal(buff, held, weapon));
}
