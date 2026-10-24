namespace InfinityMercsApp.ViewModels.Season.Loot.Items;

/// <summary>A button for rolling with this many of the box's dice, and the results they can give.</summary>
public sealed record LootDiceChoice(int Dice, bool IsEnabled)
{
    public string Label => Dice == 1 ? "1 DIE" : $"{Dice} DICE";

    /// <summary>"7–12".</summary>
    public string Range => $"{(6 * (Dice - 1)) + 1}–{6 * Dice}";
}
