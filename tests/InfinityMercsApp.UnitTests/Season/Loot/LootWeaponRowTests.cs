using InfinityMercsApp.Domain.Season.Loot;

namespace InfinityMercsApp.UnitTests.Season.Loot;

public sealed class LootWeaponRowTests
{
    private static List<string> Bands(int roll) =>
        LootWeaponChart.For(roll).Distance()?.Bands.Select(band => $"{band.Name} {band.Max} {band.Mod}").ToList() ?? [];

    // The sheet's Spitfire, in 8" columns, matches the army data's Spitfire bands.
    [Fact]
    public void Distance_Spitfire_MergesColumnsIntoTheArmyBands() =>
        Assert.Equal(["short 20 0", "med 60 +3", "long 80 -3", "max 120 -6"], Bands(21));

    [Fact]
    public void Distance_SniperRifle_ReachesTheLastColumn() =>
        Assert.Equal(["short 20 -3", "med 40 0", "long 120 +3", "max 240 -3"], Bands(23));

    [Fact]
    public void Distance_TwoBands_AreShortAndMax() =>
        Assert.Equal(["short 20 0", "max 40 -3"], Bands(3));

    [Fact]
    public void Distance_CcWeapon_IsNull() => Assert.Null(LootWeaponChart.For(1).Distance());

    [Fact]
    public void Rows_Sheet_CoverEveryRollOnce() =>
        Assert.Equal(Enumerable.Range(1, 24), LootWeaponChart.Rows.Select(row => row.Roll));
}
