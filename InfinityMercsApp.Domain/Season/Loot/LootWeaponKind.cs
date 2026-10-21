namespace InfinityMercsApp.Domain.Season.Loot;

/// <summary>What a row of the Lootsheet's Weapons tab gives.</summary>
public enum LootWeaponKind
{
    /// <summary>A weapon.</summary>
    Weapon,

    /// <summary>An "Upgrade" row: an augment of its own, with the row's traits (user's ruling, 2026-10-19).</summary>
    Upgrade,

    /// <summary>Armed Turret: an augment carrying a weapon rolled on 4d6, which must have range bands.</summary>
    ArmedTurret,

    /// <summary>EXTRA/REROLL: a free extra roll on the same tab (user's ruling, 2026-10-19).</summary>
    Extra
}
