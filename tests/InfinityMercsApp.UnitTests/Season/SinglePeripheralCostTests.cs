using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Domain.Models.Filtering;

namespace InfinityMercsApp.UnitTests.Season;

public sealed class SinglePeripheralCostTests
{
    // A unit fielding Bots: the Bot group's option costs 9 for 3 minis, so 3 points per peripheral.
    private static readonly UnitProfileGroups Unit = new(
    [
        new ProfileGroup { Isc = "Controller", Options = [new ProfileOption { Name = "Controller", Cost = "30", Minis = 1 }] },
        new ProfileGroup { Isc = "Bot", Options = [new ProfileOption { Name = "Bot", Cost = "-" }, new ProfileOption { Name = "Bots", Cost = "9", Minis = 3 }] }
    ]);

    [Theory]
    [InlineData("30", "Bot", 3, "24")]
    [InlineData("30", "Bot", 1, "30")]
    [InlineData("4", "Bot", 3, "0")]
    [InlineData("-", "Bot", 3, "-")]
    [InlineData("30", "Drone", 3, "30")]
    public void DropsOnePeripheralCostPerExtraPeripheral(string cost, string peripheral, int count, string expected) =>
        Assert.Equal(expected, UnitOptionFilter.SinglePeripheralCost(Unit, cost, peripheral, count));
}
