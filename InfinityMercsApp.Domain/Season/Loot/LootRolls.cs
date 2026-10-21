namespace InfinityMercsApp.Domain.Season.Loot;

/// <summary>
/// A Lootsheet roll: up to 4 of the box's dice are set aside, only the last is rolled and the others count as 6s,
/// so N dice land from 6(N-1)+1 to 6N.
/// </summary>
public static class LootRolls
{
    public const int MaxDice = 4;

    /// <summary>The Armed Turret's weapon is rolled on this many dice, summed.</summary>
    public const int TurretDice = 4;

    public static int Result(int dice, int lastDie) => (6 * (dice - 1)) + lastDie;

    /// <summary>Every result <paramref name="dice"/> dice can give.</summary>
    public static IEnumerable<int> Results(int dice) => Enumerable.Range((6 * (dice - 1)) + 1, 6);
}
