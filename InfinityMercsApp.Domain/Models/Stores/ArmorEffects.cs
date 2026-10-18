using InfinityMercsApp.Domain.Season.RecruitRolls;

namespace InfinityMercsApp.Domain.Models.Stores;

/// <summary>
/// What a piece of armour does beyond its troop type and ARM/BTS, as listed in the market's armour table.
/// Values set here replace the trooper's underlying ones; bonuses from perks and the like still add on top.
/// </summary>
public sealed class ArmorEffects
{
    /// <summary>Steps along Dogged → NWI → +1 VITA/STR, on top of any from becoming HI.</summary>
    public int WoundsSkill { get; set; }

    /// <summary>A VITA/STR that replaces the trooper's, and any wounds skill with it (Superheavy Armor).</summary>
    public int? Vitality { get; set; }

    /// <summary>A MOV in inches that replaces the trooper's, e.g. "6-4".</summary>
    public string? Move { get; set; }

    public int? Silhouette { get; set; }

    /// <summary>A PH that replaces the trooper's (Power Suit's PH=15).</summary>
    public int? Ph { get; set; }

    /// <summary>A BTS the trooper can never be raised above (Teseum Plate Armor).</summary>
    public int? MaxBts { get; set; }

    /// <summary>Levels of Martial Arts gained; a trooper without it gains MA1.</summary>
    public int MartialArtsLevels { get; set; }

    public IReadOnlyList<ArmorSkill> Skills { get; set; } = [];

    public IReadOnlyList<string> Equipment { get; set; } = [];

    public IReadOnlyList<string> Characteristics { get; set; } = [];

    /// <summary>Characteristics the armour takes away, e.g. Teseum Plate's Hackable.</summary>
    public IReadOnlyList<string> CharacteristicsLost { get; set; } = [];

    /// <summary>
    /// The table each copy rolls on once when bought (Chemtank's Metachemistry, the Wetsuit's Booty); the copy keeps
    /// its result wherever it goes.
    /// </summary>
    public RecruitRollTable? Roll { get; set; }

    /// <summary>Belts and Pouches: one more equipment slot, of any kind but Armor.</summary>
    public bool ExtraSlot { get; set; }

    /// <summary>
    /// Cuckoo Virus Infusion: no Primary slot, and no attacks that need a BS roll (templates, thrown and technical
    /// weapons still work).
    /// </summary>
    public bool NoPrimary { get; set; }
}
