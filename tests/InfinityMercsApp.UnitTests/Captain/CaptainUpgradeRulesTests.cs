using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.UnitTests.Captain;

public sealed class CaptainUpgradeRulesTests
{
    [Theory]
    [InlineData(10, 18)]
    [InlineData(28, 0)]
    [InlineData(35, 0)]
    public void StartingExperience_IsWhatTheUnitCostsUnder28(int unitCost, int expected) =>
        Assert.Equal(expected, CaptainUpgradeRules.StartingExperience(unitCost));

    [Fact]
    public void StartingExperience_UsesTheOverrideWhenGiven() =>
        Assert.Equal(5, CaptainUpgradeRules.StartingExperience(10, overrideExperience: 5));

    [Fact]
    public void StatOptions_AccumulateCostAcrossTiers()
    {
        var options = CaptainUpgradeRules.StatOptions(Stat.Cc, 14);
        Assert.Equal<(int, int, int)>(
            [(0, 0, 0), (1, 2, 2), (2, 5, 5), (3, 10, 10)],
            options.Select(o => (o.Tier, o.Bonus, o.Cost)));
    }

    [Fact]
    public void StatOptions_ClampToTheHardCapAndStopThere()
    {
        // PH caps at 14: from 13, tier 1 reaches 14 and no further tiers are offered.
        var options = CaptainUpgradeRules.StatOptions(Stat.Ph, 13);
        Assert.Equal<(int, int, int)>([(0, 0, 0), (1, 1, 2)], options.Select(o => (o.Tier, o.Bonus, o.Cost)));

        // From 12, tier 2 would give +3 but is clamped to +2.
        Assert.Equal<int>([0, 1, 2], CaptainUpgradeRules.StatOptions(Stat.Ph, 12).Select(o => o.Bonus));
    }

    [Fact]
    public void StatOptions_AtTheCapOfferOnlyNoUpgrade()
    {
        Assert.Single(CaptainUpgradeRules.StatOptions(Stat.Vita, 2));
        Assert.Single(CaptainUpgradeRules.StatOptions(Stat.Wip, 15));
    }

    [Fact]
    public void StatOptions_ForAnUnknownStatOfferOnlyNoUpgrade() =>
        Assert.Equal<int>([0], CaptainUpgradeRules.StatOptions(Stat.Mov, 4).Select(o => o.Tier));

    [Fact]
    public void StatOptions_IgnoreCase() =>
        Assert.Equal(4, CaptainUpgradeRules.StatOptions(Stat.Bts, 3).Count);
}
