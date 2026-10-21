namespace InfinityMercsApp.Domain.Season.Loot;

/// <summary>The Lootsheet's Buffs tab, rolls 1 to 24.</summary>
public static class LootBuffChart
{
    public const string AntiMaterial = "Anti-material";
    public const string NonLethal = "Non-Lethal";

    public const int ExperimentalRoll = 7;
    public const int RepeatingRoll = 11;
    public const int MultiRoll = 12;
    public const int ContinuousRoll = 18;
    public const int PlasmaRoll = 21;
    public const int ExtraRoll = 24;

    public static IReadOnlyList<LootBuff> Rows { get; } =
    [
        new(1, "Shock", [], Ammo: "Shock"),
        new(2, "Stun", ["State: Stunned"]),
        new(3, "High Caliber", [], PsChange: -1),
        new(4, "Healing", [NonLethal, "Acts as Medikit"], IsNonLethal: true,
            Clarification: "T2 is equivalent to 2W. Continuous will allow healing up to max W. Can be gained in conjunction with Repairing."),
        new(5, "Repairing", [NonLethal, "Acts as Gizmokit"], IsNonLethal: true,
            Clarification: "T2 is equivalent to 2W. Continuous will allow healing up to max STR. Can be gained in conjunction with Healing."),
        new(6, "Silent", ["Silent (-6)"]),
        new(ExperimentalRoll, "Experimental", [], Saving: LootSaving.Opposite,
            Clarification: "The SR Attrib. is changed. If ARM it becomes BTS and if BTS it becomes ARM."),
        new(8, "Armor Piercing", [], Saving: LootSaving.Halved),
        new(9, "Viral", ["Bioweapon"], Ammo: "DA+Shock", Clarification: "This does not change the SR Attrib."),
        new(10, "Dual Action", [AntiMaterial], SavingNum: 2),
        new(RepeatingRoll, "Repeating", [], BurstBonus: 1,
            Clarification: "This stacks with previous Repeating Buffs. If a trooper already has +1B this weapon may be treated as +2B."),
        new(MultiRoll, "Multi", []),
        new(13, "Targeting", ["State: Targeted"]),
        new(14, "K1", [AntiMaterial], PsSet: 7, Saving: LootSaving.Zeroed),
        new(15, "Explosive", [AntiMaterial], SavingNum: 3),
        new(16, "Paralyzing", [NonLethal], Ammo: "PARA", Saving: LootSaving.PhMinusSix, IsNonLethal: true,
            Clarification: "PARA and E/M in conjunction force 3 saves: 1 PH-6 that can cause PARA effects and 2 BTS/2 that can cause E/M effects."),
        new(17, "Electromagnetic", [NonLethal], Ammo: "E/M", Saving: LootSaving.BtsHalved, SavingNum: 2, IsNonLethal: true,
            Clarification: "PARA and E/M in conjunction force 3 saves: 1 PH-6 that can cause PARA effects and 2 BTS/2 that can cause E/M effects."),
        new(ContinuousRoll, "Continuous", ["Continuous Damage"]),
        new(19, "Teseum", [AntiMaterial], Ammo: "T2"),
        new(20, "Impact", ["Impact Template (Circular)"]),
        new(PlasmaRoll, "Plasma", [], Saving: LootSaving.ArmAndBts,
            Clarification: "If rolled after Experimental, Plasma is kept and Experimental is rerolled."),
        new(22, "Smart", ["BS Weapon (WIP)"]),
        new(23, "Propulsive", ["BS Weapon (PH)"]),
        new(ExtraRoll, "Extra Roll", [])
    ];

    public static LootBuff For(int roll) => Rows.First(row => row.Roll == roll);
}
