using InfinityMercsApp.Domain.Models.Season.Gear;

namespace InfinityMercsApp.Domain.Season.Loot;

/// <summary>The Lootsheet's Weapons tab, rolls 1 to 24.</summary>
public static class LootWeaponChart
{
    /// <summary>Armed Turret's row.</summary>
    public const int ArmedTurretRoll = 18;

    public static IReadOnlyList<LootWeaponRow> Rows { get; } =
    [
        Weapon(1, "CC Weapon", [], ["CC"], "1", "8", GearSlot.Sidearm),
        Weapon(2, "Pistol", ["+3", "0", "-6"], [], "2", "7", GearSlot.Sidearm),
        Weapon(3, "Grenades", ["0", "-3"], ["Speculative Attack", "BS Weapon (PH)", "Impact Template (Circular)"], "1", "7", GearSlot.Sidearm),
        Weapon(4, "Mines", [], ["Intuitive Attack", "Concealed", "Disposable (3)", "Direct Template (Small Teardrop)", "Deployable"], "1", "7", GearSlot.Secondary),
        Weapon(5, "Small Templater", [], ["Intuitive Attack", "Direct Template (Large Teardrop)"], "1", "7", GearSlot.Sidearm),
        Upgrade(6, "Upgrade Total Control", ["DA Ammo", "Non-Lethal", "State: POS/NORMAL"]),
        Weapon(7, "Rifle", ["+3", "+3", "-3", "-3", "-6", "-6"], [], "3", "7", GearSlot.Primary),
        Weapon(8, "Disposable Weapon", ["-3", "0", "+3", "+3", "-3", "-3"], ["Disposable (2)"], "1", "6", GearSlot.Secondary),
        Weapon(9, "Shotgun", ["+6", "0", "-3"], [], "2", "6", GearSlot.Primary),
        Upgrade(10, "Upgrade Trinity", ["Target suffers 1 Wound for each failed SR", "Gives Attacker a mod of +3"], "3", "6"),
        Weapon(11, "Grenade Launcher", ["0", "0", "-3", "-6"], ["Speculative Attack", "Impact Template (Circular)"], "1", "7", GearSlot.Primary, 0.5m),
        Upgrade(12, "Upgrade Carbonite", ["DA Ammo", "State: IMM-B", "Non-Lethal"]),
        Upgrade(13, "Upgrade Oblivion", ["AP Ammo", "Non-Lethal", "State: Isolated"]),
        Weapon(14, "Perimeter", [], ["Disposable (2)", "Boost", "Perimeter", "Deployable"], "1", "7", GearSlot.Secondary),
        Weapon(15, "Marksman Rifle", ["-3", "+3", "+3", "-3", "-6", "-6"], [], "3", "7", GearSlot.Primary),
        Upgrade(16, "Upgrade Spotlight", ["AP Ammo", "State: Targeted", "Non-Lethal"]),
        Weapon(17, "Large Templater", [], [], "1", "6", GearSlot.Primary),
        new(ArmedTurretRoll, "Armed Turret", LootWeaponKind.ArmedTurret, [], [], string.Empty, string.Empty, GearSlot.Augments, 0),
        Weapon(19, "Dispenser", ["0", "+3", "-3", "-6", "-6", "-6"], ["Double Shot", "Speculative Attack", "Disposable (2)", "Targetless"], "1", "7", GearSlot.Primary, 1m),
        Weapon(20, "Thunderbolt", ["-3", "0", "+3", "+3", "0", "0"], [], "2", "6", GearSlot.Primary),
        Weapon(21, "Spitfire", ["0", "+3", "+3", "-3", "-6", "-6"], [], "4", "6", GearSlot.Primary, 1.5m),
        Weapon(22, "Heavy Machine Gun", ["-3", "0", "+3", "+3", "-3", "-3"], [], "4", "5", GearSlot.Primary, 1.5m),
        Weapon(23, "Sniper Rifle", ["-3", "0", "+3", "+3", "+3", "+3", "-3"], [], "2", "5", GearSlot.Primary, 1.5m),
        new(24, "Extra Roll", LootWeaponKind.Extra, [], [], string.Empty, string.Empty, GearSlot.Primary, 0)
    ];

    public static LootWeaponRow For(int roll) => Rows.First(row => row.Roll == roll);

    private static LootWeaponRow Weapon(
        int roll, string name, IReadOnlyList<string> rangeMods, IReadOnlyList<string> traits, string burst, string ps, GearSlot slot, decimal swc = 0) =>
        new(roll, name, LootWeaponKind.Weapon, rangeMods, traits, burst, ps, slot, swc);

    private static LootWeaponRow Upgrade(int roll, string name, IReadOnlyList<string> traits, string burst = "", string ps = "") =>
        new(roll, name, LootWeaponKind.Upgrade, [], traits, burst, ps, GearSlot.Augments, 0);
}
