using InfinityMercsApp.Domain.Season.Loot;

namespace InfinityMercsApp.Domain.Models.Season;

/// <summary>
/// A loot box the organiser gave the company, and how far it has been opened. Each roll is saved as it is made.
/// The weapon is rolled first, then buffs while dice are left; a 24 on either tab owes a free roll instead.
/// </summary>
public class SeasonLootBox
{
    public LootBoxTier Tier { get; set; }

    /// <summary>The rounds played when the box was gained (0 before the first contract).</summary>
    public int GainedAfterRound { get; set; }

    public int DiceLeft { get; set; }

    /// <summary>The weapon roll; null until one other than a 24 is rolled.</summary>
    public SeasonLootRoll? Weapon { get; set; }

    /// <summary>For an Armed Turret, the 4d6 roll on the Weapons tab for its weapon.</summary>
    public int? TurretWeapon { get; set; }

    public List<SeasonLootRoll> Buffs { get; set; } = [];

    /// <summary>Free rolls owed by 24s, taken before any more dice are spent.</summary>
    public int FreeRolls { get; set; }

    /// <summary>Fully opened: its item is in the company's inventory.</summary>
    public bool IsOpened { get; set; }
}
