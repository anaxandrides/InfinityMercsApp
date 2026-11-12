using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.SpecOps;
using InfinityMercsApp.UnitTests.Support;

namespace InfinityMercsApp.UnitTests.SpecOps;

public sealed class SpecUpgradeRulesTests
{
    private static TrooperUpgrade Upgrade(string label, CaptainStatBonuses bonuses, int? moveFirst = null, params IEnumerable<string> skills) =>
        new(label, bonuses, moveFirst, moveFirst is null ? null : 10, [], skills.ToList(), []);

    [Fact]
    public void Combine_TwoUpgrades_AddsTheBonusesAndListsEachSkillOnce()
    {
        var combined = SpecUpgradeRules.Combine(
        [
            Upgrade("+1 ARM, Dogged", CaptainStatBonuses.None with { Arm = 1 }, null, "Dogged"),
            Upgrade("+3 BTS, Dogged", CaptainStatBonuses.None with { Bts = 3 }, null, "Dogged")
        ]);

        Assert.Equal(CaptainStatBonuses.None with { Arm = 1, Bts = 3 }, combined.Bonuses);
        Assert.Equal(["Dogged"], combined.Skills);
        Assert.False(combined.SetsMove);
    }

    [Fact]
    public void Combine_MoveUpgrade_SetsTheMove()
    {
        var combined = SpecUpgradeRules.Combine([Upgrade("MOV 15-10, Dodge", CaptainStatBonuses.None, 15, "Dodge")]);

        Assert.Equal(15, combined.MoveFirstCm);
        Assert.Equal(10, combined.MoveSecondCm);
    }

    [Fact]
    public void PurchaseLine_Upgrade_ReadsAsASpecOpsPurchase() =>
        Assert.Equal("Spec-Ops: +1 WIP", Upgrade("+1 WIP", CaptainStatBonuses.None with { Wip = 1 }).PurchaseLine);

    [Theory]
    [InlineData(SpecialOperativesData.SpecOpsId, SpecialOperativeKind.SpecOps)]
    [InlineData(SpecialOperativesData.TeamOpsId, SpecialOperativeKind.TeamOps)]
    public void KindOf_OperativeUnit_ReadsItsSkill(int unitId, SpecialOperativeKind expected)
    {
        var unit = unitId == SpecialOperativesData.SpecOpsId ? SpecialOperativesData.SpecOps(101) : SpecialOperativesData.TeamOps(101);
        var skills = SpecialOperativesData.WithNames(ArmyFilterCatalog.Empty).Names(ArmyFilterSection.Skills);

        Assert.Equal(expected, SpecialOperatives.KindOf(unit.ProfileGroups, skills));
    }

    [Fact]
    public void KindOf_SkillsUnnamed_IsNone() =>
        Assert.Equal(SpecialOperativeKind.None, SpecialOperatives.KindOf(SpecialOperativesData.SpecOps(101).ProfileGroups, new Dictionary<int, string>()));
}
