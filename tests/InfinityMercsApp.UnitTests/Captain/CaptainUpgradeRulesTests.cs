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
        var options = CaptainUpgradeRules.StatOptions(Stat.Cc, 14, CaptainStatChart.SpecOps);
        Assert.Equal<(int, int, int)>(
            [(0, 0, 0), (1, 2, 2), (2, 5, 5), (3, 10, 10)],
            options.Select(o => (o.Tier, o.Bonus, o.Cost)));
    }

    [Fact]
    public void StatOptions_ClampToTheHardCapAndStopThere()
    {
        // PH caps at 14: from 13, tier 1 reaches 14 and no further tiers are offered.
        var options = CaptainUpgradeRules.StatOptions(Stat.Ph, 13, CaptainStatChart.SpecOps);
        Assert.Equal<(int, int, int)>([(0, 0, 0), (1, 1, 2)], options.Select(o => (o.Tier, o.Bonus, o.Cost)));

        // From 12, tier 2 would give +3 but is clamped to +2.
        Assert.Equal<int>([0, 1, 2], CaptainUpgradeRules.StatOptions(Stat.Ph, 12, CaptainStatChart.SpecOps).Select(o => o.Bonus));
    }

    [Fact]
    public void StatOptions_AtTheCapOfferOnlyNoUpgrade()
    {
        Assert.Single(CaptainUpgradeRules.StatOptions(Stat.Vita, 2, CaptainStatChart.SpecOps));
        Assert.Single(CaptainUpgradeRules.StatOptions(Stat.Wip, 15, CaptainStatChart.SpecOps));
    }

    [Fact]
    public void StatOptions_ForAnUnknownStatOfferOnlyNoUpgrade() =>
        Assert.Equal<int>([0], CaptainUpgradeRules.StatOptions(Stat.Mov, 4, CaptainStatChart.SpecOps).Select(o => o.Tier));

    [Fact]
    public void SpecOps_BtsHasNoCap() =>
        Assert.Equal(4, CaptainUpgradeRules.StatOptions(Stat.Bts, 3, CaptainStatChart.SpecOps).Count);

    /// <summary>Stat, base value, and the (tier, bonus, total XP) the Mercenaries chart offers.</summary>
    public static TheoryData<Stat, int, IReadOnlyList<(int Tier, int Bonus, int Cost)>> MercenariesTiers => new()
    {
        { Stat.Cc, 13, [(0, 0, 0), (1, 2, 2), (2, 5, 5), (3, 10, 10)] },
        { Stat.Bs, 12, [(0, 0, 0), (1, 1, 2), (2, 2, 5), (3, 3, 10)] },
        { Stat.Bs, 14, [(0, 0, 0), (1, 1, 2)] },
        { Stat.Ph, 10, [(0, 0, 0), (1, 1, 2), (2, 2, 5), (3, 3, 10)] },
        { Stat.Wip, 13, [(0, 0, 0), (1, 1, 2), (2, 2, 5)] },
        { Stat.Arm, 1, [(0, 0, 0), (1, 1, 5), (2, 3, 10)] },
        { Stat.Bts, 0, [(0, 0, 0), (1, 3, 2), (2, 6, 4), (3, 9, 6)] },
        { Stat.Bts, 3, [(0, 0, 0), (1, 3, 2), (2, 6, 4)] },
        { Stat.Vita, 1, [(0, 0, 0), (1, 1, 10)] }
    };

    [Theory]
    [MemberData(nameof(MercenariesTiers))]
    public void Mercenaries_FollowTheRulesChartAndItsCaps(Stat stat, int baseValue, IReadOnlyList<(int Tier, int Bonus, int Cost)> expected) =>
        Assert.Equal(expected, CaptainUpgradeRules.StatOptions(stat, baseValue, CaptainStatChart.Mercenaries).Select(o => (o.Tier, o.Bonus, o.Cost)));

    [Fact]
    public void TheChartsDifferForPhAndWip()
    {
        Assert.Equal<int>([0, 1, 3], CaptainUpgradeRules.StatOptions(Stat.Ph, 10, CaptainStatChart.SpecOps).Select(o => o.Bonus));
        Assert.Equal<int>([0, 1, 3, 6], CaptainUpgradeRules.StatOptions(Stat.Wip, 8, CaptainStatChart.SpecOps).Select(o => o.Bonus));
        Assert.Equal<int>([0, 1, 2, 3], CaptainUpgradeRules.StatOptions(Stat.Wip, 8, CaptainStatChart.Mercenaries).Select(o => o.Bonus));
    }
}
