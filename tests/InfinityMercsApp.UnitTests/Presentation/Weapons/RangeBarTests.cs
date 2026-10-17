using InfinityMercsApp.Domain.Models.Metadata.Ranges;
using InfinityMercsApp.Presentation.Weapons;

namespace InfinityMercsApp.UnitTests.Presentation.Weapons;

public sealed class RangeBarTests
{
    // A Combi Rifle: +3 to 20 cm, 0 to 40 cm, -3 to 60 cm, -6 to 80 cm.
    private static WeaponDistance CombiRifle() => new()
    {
        Bands =
        [
            new WeaponDistanceBand { Name = WeaponDistance.ShortBand, Max = 20, Mod = "3" },
            new WeaponDistanceBand { Name = WeaponDistance.MediumBand, Max = 40, Mod = "0" },
            new WeaponDistanceBand { Name = WeaponDistance.LongBand, Max = 60, Mod = "-3" },
            new WeaponDistanceBand { Name = WeaponDistance.MaximumBand, Max = 80, Mod = "-6" }
        ]
    };

    [Fact]
    public void BandsSpanTheBarInOrder()
    {
        var bar = RangeBar.From(CombiRifle(), xVisor: false, inInches: true);

        Assert.Equal(
            [
                new RangeBarBand(0, 0.25, "3", RangeBandTone.Bonus),
                new RangeBarBand(0.25, 0.5, "0", RangeBandTone.Neutral),
                new RangeBarBand(0.5, 0.75, "-3", RangeBandTone.Penalty),
                new RangeBarBand(0.75, 1, "-6", RangeBandTone.Severe)
            ],
            bar.Bands);
    }

    [Fact]
    public void EveryBandEdgeIsMarked()
    {
        Assert.Equal(
            ["0\"", "8\"", "16\"", "24\"", "32\""],
            RangeBar.From(CombiRifle(), xVisor: false, inInches: true).Marks.Select(mark => mark.Label));
        Assert.Equal(
            ["0cm", "20cm", "40cm", "60cm", "80cm"],
            RangeBar.From(CombiRifle(), xVisor: false, inInches: false).Marks.Select(mark => mark.Label));
    }

    [Fact]
    public void AnXVisorLessensPenalties() =>
        Assert.Equal(
            ["3", "0", "0", "-3"],
            RangeBar.From(CombiRifle(), xVisor: true, inInches: true).Bands.Select(band => band.Modifier));

    [Theory]
    [InlineData("-1", "0")]
    [InlineData("-6", "-3")]
    [InlineData("2", "2")]
    [InlineData("PH", "PH")]
    public void XVisorModifiers(string modifier, string expected) => Assert.Equal(expected, RangeBar.WithXVisor(modifier));

    [Fact]
    public void NoDistanceShowsNothing()
    {
        Assert.Same(RangeBar.Empty, RangeBar.From(null, false, true));
        Assert.Same(RangeBar.Empty, RangeBar.From(new WeaponDistance(), false, true));
    }
}
