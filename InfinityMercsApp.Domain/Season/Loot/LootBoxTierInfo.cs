namespace InfinityMercsApp.Domain.Season.Loot;

/// <summary>
/// A loot box tier as the rules list it: its name, rarity, the dice it holds, and the renown of a weapon it gives.
/// </summary>
public sealed record LootBoxTierInfo(LootBoxTier Tier, string Name, string Rarity, int Dice, int Renown)
{
    /// <summary>"Black Market Cache (Rare, 6 dice)".</summary>
    public string Display => $"{Name} ({Rarity}, {Dice} dice)";
}
