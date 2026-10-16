using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.UnitTests.Captain;

public sealed class CaptainBuildValidationTests
{
    private static readonly IReadOnlyList<string> Weapons = ["(2) - Sniper Rifle", "(1) - Grenades"];

    private static CaptainBuild Build(
        int startingExperience = 10,
        IReadOnlyList<CaptainStatPick>? stats = null,
        IReadOnlyList<string>? weapons = null) =>
        new(
            startingExperience,
            stats ?? [new CaptainStatPick(Stat.Bs, 12, 0, CaptainStatChart.Mercenaries)],
            [new CaptainChoicePicks("Weapons", Weapons, weapons ?? [CaptainChoiceLabel.None, CaptainChoiceLabel.None, CaptainChoiceLabel.None])]);

    private static IReadOnlyList<CaptainBuildProblemKind> Kinds(CaptainBuild build) =>
        CaptainBuildValidation.Problems(build).Select(problem => problem.Kind).ToList();

    [Fact]
    public void AnUntouchedCaptainHasNoProblems() => Assert.Empty(CaptainBuildValidation.Problems(Build()));

    [Fact]
    public void TheCostCountsStatTiersAndFilledSlots()
    {
        var build = Build(stats: [new CaptainStatPick(Stat.Bs, 12, 2, CaptainStatChart.Mercenaries)], weapons: ["(2) - Sniper Rifle", "(1) - Grenades", CaptainChoiceLabel.None]);

        Assert.Equal(5 + 2 + 1, build.SpentExperience);
        Assert.Equal(2, build.ExperienceRemaining);
    }

    [Fact]
    public void SpendingMoreThanTheBudgetIsAProblem()
    {
        var problems = CaptainBuildValidation.Problems(Build(startingExperience: 1, weapons: ["(2) - Sniper Rifle", CaptainChoiceLabel.None, CaptainChoiceLabel.None]));

        Assert.Equal([new CaptainBuildProblem(CaptainBuildProblemKind.Overspent, "1 XP over budget.")], problems);
    }

    [Fact]
    public void TheSameChoiceInTwoSlotsIsAProblem()
    {
        var problems = CaptainBuildValidation.Problems(Build(weapons: ["(2) - Sniper Rifle", "(2) - sniper rifle", "(1) - Grenades"]));

        Assert.Equal([new CaptainBuildProblem(CaptainBuildProblemKind.DuplicateChoice, "Sniper Rifle is chosen more than once in Weapons.")], problems);
    }

    [Fact]
    public void AChoiceThatIsNotOfferedIsAProblem() =>
        Assert.Equal([CaptainBuildProblemKind.ChoiceNotOffered], Kinds(Build(weapons: ["(0) - Spitfire", CaptainChoiceLabel.None, CaptainChoiceLabel.None])));

    [Theory]
    [InlineData(Stat.Ph, 14, 1)]
    [InlineData(Stat.Vita, 2, 1)]
    [InlineData(Stat.Bs, 12, 4)]
    [InlineData(Stat.Bts, 6, 2)]
    public void ATierPastTheCapOrTheChartIsAProblem(Stat stat, int baseValue, int tier)
    {
        var problems = CaptainBuildValidation.Problems(Build(stats: [new CaptainStatPick(stat, baseValue, tier, CaptainStatChart.Mercenaries)]));

        Assert.Equal([new CaptainBuildProblem(CaptainBuildProblemKind.StatTierNotOffered, $"{stat.Key()} can't take upgrade tier {tier}.")], problems);
    }

    [Fact]
    public void EveryProblemIsListed() =>
        Assert.Equal(
            [CaptainBuildProblemKind.Overspent, CaptainBuildProblemKind.StatTierNotOffered, CaptainBuildProblemKind.DuplicateChoice],
            Kinds(Build(startingExperience: 0, stats: [new CaptainStatPick(Stat.Vita, 2, 1, CaptainStatChart.Mercenaries)], weapons: ["(1) - Grenades", "(1) - Grenades", CaptainChoiceLabel.None])));
}
