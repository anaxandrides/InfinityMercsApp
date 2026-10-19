namespace InfinityMercsApp.Domain.Models.Season;

/// <summary>One roll on a Lootsheet tab: the dice it used (the last rolled, the others counted as 6s) and the result.</summary>
public class SeasonLootRoll
{
    public int Dice { get; set; }

    public int Result { get; set; }
}
