using InfinityMercsApp.Domain.Season.Inducements;

namespace InfinityMercsApp.UnitTests.Season.Inducements;

public sealed class HiredTroopOfferTests
{
    private static HiredTroopOffer Row(string profile) => HiredTroopChart.Offers.First(offer => offer.Profile == profile);

    [Fact]
    public void Allows_SmgOrBsgRow_TakesOnlyThoseLoadouts()
    {
        var row = Row("SMG or BSG");

        Assert.True(row.Allows(["Submachine Gun", "Breaker Pistol"]));
        Assert.True(row.Allows(["Boarding Shotgun", "Pistol"]));
        Assert.False(row.Allows(["Spitfire", "Boarding Pistol"]));
    }

    [Fact]
    public void Allows_AnyRow_TakesEveryLoadout() => Assert.True(Row("Any").Allows(["Rifle"]));

    [Fact]
    public void Display_RestrictedRow_NamesTheProfile() =>
        Assert.Equal("Authorized Bounty Hunter (Sniper or Spitfire)", Row("Sniper or Spitfire").Display);
}
