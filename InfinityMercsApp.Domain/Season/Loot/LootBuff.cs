namespace InfinityMercsApp.Domain.Season.Loot;

/// <summary>
/// A row of the Lootsheet's Buffs tab and what it does to the weapon: <paramref name="Traits"/> are listed on the
/// weapon as written; the rest change its profile. <paramref name="SavingNum"/> is the saving rolls it forces (0
/// when unchanged), and <paramref name="PsSet"/> a PS it sets outright.
/// </summary>
public sealed record LootBuff(
    int Roll,
    string Name,
    IReadOnlyList<string> Traits,
    string? Ammo = null,
    int BurstBonus = 0,
    int PsChange = 0,
    int? PsSet = null,
    LootSaving Saving = LootSaving.Unchanged,
    int SavingNum = 0,
    bool IsNonLethal = false,
    string Clarification = "")
{
    /// <summary>EXTRA/REROLL: a free extra roll on this tab.</summary>
    public bool IsExtra => Roll == LootBuffChart.ExtraRoll;

    /// <summary>Repeating stacks, so it may be rolled more than once.</summary>
    public bool Stacks => Roll == LootBuffChart.RepeatingRoll;

    /// <summary>Multi: the weapon gets two profiles, B as it is (AP or Shock) and B1 (DA and Anti-material).</summary>
    public bool IsMulti => Roll == LootBuffChart.MultiRoll;
}
