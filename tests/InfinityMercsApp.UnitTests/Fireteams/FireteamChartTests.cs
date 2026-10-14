using InfinityMercsApp.Domain.Models.Army.Profiles;

namespace InfinityMercsApp.UnitTests.Fireteams;

public sealed class FireteamChartTests
{
    [Fact]
    public void TypeCountsIgnoreCaseButNothingElse() =>
        Assert.Equal(new FireteamTypeCounts(1, 2, 0), new FireteamTeam { TypeList = ["duo", "HARIS", "Haris", " CORE", "1"] }.TypeCounts);

    [Theory]
    [InlineData(FireteamType.Core, "1")]
    [InlineData(FireteamType.Haris, "T")]
    [InlineData(FireteamType.Duo, "-")]
    public void SpecCountsShowTForUnlimitedAndADashWhenMissing(FireteamType type, string expected)
    {
        var chart = new FireteamChart { Spec = [new("core", 1), new("HARIS", 6), new("DUO", null), new("DUO", 2)] };

        Assert.Equal(expected, chart.SpecDisplay(type));
    }
}
