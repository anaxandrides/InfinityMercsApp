using InfinityMercsApp.Domain.Season;

namespace InfinityMercsApp.UnitTests.Season;

public sealed class InjuryRulesTests
{
    [Theory]
    [InlineData("14", 11)]
    [InlineData("3", 0)]
    [InlineData("-", null)]
    [InlineData(null, null)]
    public void AvoidTargetIsPhMinusThree(string? ph, int? expected) => Assert.Equal(expected, InjuryRules.AvoidTarget(ph));

    [Theory]
    [InlineData(1, "Battle Fury")]
    [InlineData(6, "Battle Fury")]
    [InlineData(7, "Punctured Lung")]
    [InlineData(12, "Brain Injury")]
    [InlineData(20, "Shell Shocked")]
    public void EveryRollMapsToAnInjury(int roll, string injury) =>
        Assert.Equal(injury, InjuryRules.Table[InjuryRules.TableIndexForRoll(roll)].Label);

    [Fact]
    public void OffTableRollsMapToNothing()
    {
        Assert.Equal(-1, InjuryRules.TableIndexForRoll(0));
        Assert.Equal(-1, InjuryRules.TableIndexForRoll(21));
        Assert.Equal("7-8 | Punctured Lung", InjuryRules.Table[1].ToString());
    }
}
