using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Loot;
using InfinityMercsApp.Services.Rolls;

namespace InfinityMercsApp.Services.Season.Loot;

/// <summary>
/// Opens a loot box roll by roll. The weapon comes first; while dice are left each further roll is a buff. A roll
/// takes 1 to 4 dice: the last is rolled and the others count as 6s. A 24 owes a free roll on the same tab (user's
/// ruling, 2026-10-19), and an illegal or duplicate buff is rerolled with the same dice. The box is opened only
/// when the player takes its loot, so the last buff can still be rerolled as illegal before then.
/// </summary>
public sealed class LootBoxOpener(IDice dice)
{
    /// <summary>A new box of <paramref name="tier"/>, gained after <paramref name="roundsPlayed"/> rounds.</summary>
    public static SeasonLootBox New(LootBoxTier tier, int roundsPlayed) => new()
    {
        Tier = tier,
        GainedAfterRound = roundsPlayed,
        DiceLeft = LootBoxTiers.Of(tier).Dice
    };

    /// <summary>The most dice the next roll can take: 4 on a free roll, else up to what is left.</summary>
    public static int MaxDice(SeasonLootBox box) =>
        box.IsOpened ? 0 : box.FreeRolls > 0 ? LootRolls.MaxDice : Math.Min(LootRolls.MaxDice, box.DiceLeft);

    /// <summary>The next roll is on the Weapons tab.</summary>
    public static bool RollsWeapon(SeasonLootBox box) => box.Weapon is null;

    /// <summary>
    /// The next roll can take <paramref name="diceCount"/> dice; a buff roll also needs some buff it may give, or
    /// rerolling would never end.
    /// </summary>
    public static bool CanRoll(SeasonLootBox box, int diceCount) =>
        diceCount >= 1 && diceCount <= MaxDice(box) &&
        (RollsWeapon(box) || LootBuffRules.HasLegal(diceCount, HeldBuffs(box), WeaponRow(box)));

    /// <summary>The box's last buff can be rerolled as illegal: there is one, and another its dice can give.</summary>
    public static bool CanRerollLastBuff(SeasonLootBox box) =>
        !box.IsOpened && box.Buffs.Count > 0 &&
        LootBuffRules.HasLegal(box.Buffs[^1].Dice, HeldBuffs(box).SkipLast(1).ToList(), WeaponRow(box), [box.Buffs[^1].Result]);

    /// <summary>The loot can be taken once its weapon is rolled; dice left over are given up.</summary>
    public static bool CanTake(SeasonLootBox box) => !box.IsOpened && box.Weapon is not null;

    /// <summary>Rolls the weapon or a buff with <paramref name="diceCount"/> dice; what happened, for the log.</summary>
    public string Roll(SeasonLootBox box, int diceCount)
    {
        if (!CanRoll(box, diceCount))
        {
            return string.Empty;
        }

        var free = box.FreeRolls > 0;
        if (free)
        {
            box.FreeRolls--;
        }
        else
        {
            box.DiceLeft -= diceCount;
        }

        var prefix = $"{(RollsWeapon(box) ? "Weapon" : "Buff")} ({diceCount} {(diceCount == 1 ? "die" : "dice")}{(free ? ", free" : string.Empty)})";
        return $"{prefix}: {(RollsWeapon(box) ? RollWeapon(box, diceCount) : AddBuff(box, diceCount, []))}";
    }

    /// <summary>Rerolls the last buff, which the player found illegal, with its dice and for nothing.</summary>
    public string RerollLastBuff(SeasonLootBox box)
    {
        if (!CanRerollLastBuff(box))
        {
            return string.Empty;
        }

        var last = box.Buffs[^1];
        box.Buffs.RemoveAt(box.Buffs.Count - 1);
        return $"Rerolled {LootBuffChart.For(last.Result).Name} as illegal: {AddBuff(box, last.Dice, [last.Result])}";
    }

    /// <summary>Takes the loot: the box is opened and its item goes to the inventory.</summary>
    public static void Take(SeasonLootBox box)
    {
        if (CanTake(box))
        {
            box.IsOpened = true;
        }
    }

    private string RollWeapon(SeasonLootBox box, int diceCount)
    {
        var roll = LootRolls.Result(diceCount, dice.RollD6());
        var row = LootWeaponChart.For(roll);
        if (row.Kind == LootWeaponKind.Extra)
        {
            box.FreeRolls++;
            return $"{roll}, a free extra roll";
        }

        box.Weapon = new SeasonLootRoll { Dice = diceCount, Result = roll };
        if (row.Kind != LootWeaponKind.ArmedTurret)
        {
            return $"{roll} {row.Name}";
        }

        // The turret's weapon: 4d6 summed, rolled again until it is one with range bands.
        int turretRoll;
        do
        {
            turretRoll = Enumerable.Range(0, LootRolls.TurretDice).Sum(_ => dice.RollD6());
        }
        while (!LootWeaponChart.For(turretRoll).HasRangeBands);

        box.TurretWeapon = turretRoll;
        return $"{roll} {row.Name}, armed with {turretRoll} {LootWeaponChart.For(turretRoll).Name} (4d6)";
    }

    // Draws a legal buff for the dice; Plasma after Experimental keeps Plasma and rerolls Experimental.
    private string AddBuff(SeasonLootBox box, int diceCount, IReadOnlyCollection<int> excluded)
    {
        var weapon = WeaponRow(box);
        List<string> rerolled = [];
        var roll = DrawBuff(diceCount, HeldBuffs(box), weapon, excluded, rerolled);
        var buff = LootBuffChart.For(roll);
        var text = $"{roll} {buff.Name}{RerolledText(rerolled)}";
        if (buff.IsExtra)
        {
            box.FreeRolls++;
            return $"{roll}, a free extra roll{RerolledText(rerolled)}";
        }

        box.Buffs.Add(new SeasonLootRoll { Dice = diceCount, Result = roll });
        if (buff.Roll != LootBuffChart.PlasmaRoll ||
            box.Buffs.FirstOrDefault(held => held.Result == LootBuffChart.ExperimentalRoll) is not { } experimental)
        {
            return text;
        }

        box.Buffs.Remove(experimental);
        if (!LootBuffRules.HasLegal(experimental.Dice, HeldBuffs(box), weapon))
        {
            return $"{text}; Experimental is lost, with nothing left to reroll it into";
        }

        List<string> experimentalRerolls = [];
        var replacement = DrawBuff(experimental.Dice, HeldBuffs(box), weapon, [], experimentalRerolls);
        var replacementBuff = LootBuffChart.For(replacement);
        if (replacementBuff.IsExtra)
        {
            box.FreeRolls++;
            return $"{text}; Experimental rerolled: {replacement}, a free extra roll{RerolledText(experimentalRerolls)}";
        }

        box.Buffs.Add(new SeasonLootRoll { Dice = experimental.Dice, Result = replacement });
        return $"{text}; Experimental rerolled: {replacement} {replacementBuff.Name}{RerolledText(experimentalRerolls)}";
    }

    private int DrawBuff(int diceCount, IReadOnlyList<LootBuff> held, LootWeaponRow weapon, IReadOnlyCollection<int> excluded, List<string> rerolled)
    {
        while (true)
        {
            var roll = LootRolls.Result(diceCount, dice.RollD6());
            var buff = LootBuffChart.For(roll);
            if (!excluded.Contains(roll) && LootBuffRules.IsLegal(buff, held, weapon))
            {
                return roll;
            }

            rerolled.Add(buff.Name);
        }
    }

    private static string RerolledText(IReadOnlyList<string> rerolled) =>
        rerolled.Count == 0 ? string.Empty : $" (rerolled {string.Join(", ", rerolled)})";

    private static List<LootBuff> HeldBuffs(SeasonLootBox box) => box.Buffs.Select(buff => LootBuffChart.For(buff.Result)).ToList();

    // The row whose traits decide what is legal: an Upgrade's own, or the weapon's.
    private static LootWeaponRow WeaponRow(SeasonLootBox box) =>
        box.Weapon is { } weapon ? LootWeaponChart.For(weapon.Result) : LootWeaponChart.For(1);
}
