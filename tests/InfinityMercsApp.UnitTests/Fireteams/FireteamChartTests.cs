using InfinityMercsApp.Domain.Models.Army.Fireteams;

namespace InfinityMercsApp.UnitTests.Fireteams;

public sealed class FireteamChartTests
{
    [Fact]
    public void TypeCounts_MixedCaseAndJunk_IgnoresOnlyCase() =>
        Assert.Equal(new FireteamTypeCounts(1, 2, 0), new FireteamTeam { TypeList = ["duo", "HARIS", "Haris", " CORE", "1"] }.TypeCounts);

    [Theory]
    [InlineData(FireteamType.Core, "1")]
    [InlineData(FireteamType.Haris, "T")]
    [InlineData(FireteamType.Duo, "-")]
    public void SpecDisplay_UnlimitedOrMissing_ShowsTOrDash(FireteamType type, string expected)
    {
        var chart = new FireteamChart { Spec = [new("core", 1), new("HARIS", 6), new("DUO", null), new("DUO", 2)] };

        Assert.Equal(expected, chart.SpecDisplay(type));
    }
}
