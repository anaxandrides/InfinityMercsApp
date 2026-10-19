namespace InfinityMercsApp.Domain.Season.Loot;

/// <summary>How a buff changes the saving roll's attribute (the sheet's "SR Attrib").</summary>
public enum LootSaving
{
    Unchanged,

    /// <summary>Experimental: ARM becomes BTS and BTS becomes ARM.</summary>
    Opposite,

    /// <summary>Armor Piercing: (ARM or BTS)/2.</summary>
    Halved,

    /// <summary>Plasma: ARM and BTS.</summary>
    ArmAndBts,

    /// <summary>K1: ARM/BTS count as 0.</summary>
    Zeroed,

    /// <summary>Paralyzing: PH-6.</summary>
    PhMinusSix,

    /// <summary>Electromagnetic: BTS/2.</summary>
    BtsHalved
}
