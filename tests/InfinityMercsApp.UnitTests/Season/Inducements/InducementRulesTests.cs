using InfinityMercsApp.Domain.Season.Inducements;

namespace InfinityMercsApp.UnitTests.Season.Inducements;

public sealed class InducementRulesTests
{
    // The rules' example: Jack (90) against Jill (143) gets (143 - 90) / 2 = 26.5, rounded down to 25.
    [Fact]
    public void For_Underdog_GetsHalfTheDifferenceRoundedDownToFive() =>
        Assert.Equal(25, InducementRules.For(ownRenown: 90, opponentRenown: 143, isCooperative: false));

    [Theory]
    [InlineData(143, 90, 0)]
    [InlineData(100, 100, 0)]
    [InlineData(100, 109, 0)]
    [InlineData(100, 110, 5)]
    [InlineData(100, 160, 30)]
    public void For_RenownPair_GivesTheUnderdogsShare(int own, int opponent, int expected) =>
        Assert.Equal(expected, InducementRules.For(own, opponent, isCooperative: false));

    [Fact]
    public void For_Cooperative_GivesNothing() =>
        Assert.Equal(0, InducementRules.For(ownRenown: 90, opponentRenown: 143, isCooperative: true));

    [Theory]
    [InlineData(6, 1, 1)]
    [InlineData(5, 2, 1)]
    [InlineData(4, 2, 0)]
    [InlineData(6, 0, 0)]
    public void HiresBeyondLimit_Deployment_CountsTheHiresPastSix(int companySlots, int hired, int expected) =>
        Assert.Equal(expected, InducementRules.HiresBeyondLimit(companySlots, hired));
}
