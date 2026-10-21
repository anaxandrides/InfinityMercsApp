using InfinityMercsApp.Domain.Models.Metadata.Ranges;

namespace InfinityMercsApp.UnitTests.Stats;

public sealed class WeaponDistanceTests
{
    private static WeaponDistanceBand Band(string name, int max, string mod) => new() { Name = name, Max = max, Mod = mod };

    [Fact]
    public void GetOrderedBands_Bands_RunFromThePreviousBandsEnd()
    {
        var distance = new WeaponDistance
        {
            Bands = [Band("med", 80, "0"), Band("short", 40, "+3"), Band("xlong", 300, "-9"), Band("long", 120, "-3")]
        };

        Assert.Equal(
            [new RangeBand("short", 0, 40, "+3"), new RangeBand("med", 40, 80, "0"), new RangeBand("long", 80, 120, "-3")],
            distance.GetOrderedBands());
    }

    [Fact]
    public void Short_RepeatedBand_KeepsTheLastValue()
    {
        var shortBand = new WeaponDistance { Bands = [Band("short", 40, "+3"), Band("short", 60, "+6")] }.Short;

        Assert.NotNull(shortBand);
        Assert.Equal(60, shortBand.Max);
    }
}
