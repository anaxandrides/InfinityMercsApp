using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Domain.Models.Army.Traits;

namespace InfinityMercsApp.UnitTests.Stats;

public sealed class UnitTraitsTests
{
    private static ProfileOption OptionWithOrders(params IEnumerable<string> types) =>
        new() { Orders = types.Select(type => new OrderEntry(type, null, null)).ToList() };

    [Fact]
    public void AnOptionsOrdersGiveItsOrderTraits() =>
        Assert.Equal(new UnitOrderTraits(false, true, false, true), UnitTraits.ReadOrderTraits(OptionWithOrders("irregular", "TACTICAL", "")));

    [Theory]
    [InlineData("Killer Hacking Device", false, false, true)]
    [InlineData("Cube 2.0", false, true, false)]
    [InlineData("Cube", true, false, false)]
    [InlineData("No Cube", false, false, false)]
    [InlineData("Hackable", false, false, true)]
    public void AnOptionsNamesGiveItsTechTraits(string name, bool cube, bool cube2, bool hackable) =>
        Assert.Equal(new UnitTechTraits(cube, cube2, hackable), UnitTraits.ReadTechTraits([name]));
}
