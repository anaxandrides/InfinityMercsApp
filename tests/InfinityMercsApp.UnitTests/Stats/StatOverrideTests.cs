using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.UnitTests.Stats;

public sealed class StatOverrideTests
{
    [Theory]
    [InlineData("WIP=12", Stat.Wip, 12)]
    [InlineData(" ph = 14 ", Stat.Ph, 14)]
    [InlineData("STR=2", Stat.Str, 2)]
    public void TryParse_KeyEqualsValue_ReadsIt(string text, Stat stat, int value)
    {
        Assert.True(StatOverride.TryParse(text, out var statOverride));
        Assert.Equal(new StatOverride(stat, value), statOverride);
    }

    [Theory]
    [InlineData("Mimetism")]
    [InlineData("XP=3")]
    [InlineData("WIP=high")]
    [InlineData(null)]
    public void TryParse_AnythingElse_ReturnsFalse(string? text) => Assert.False(StatOverride.TryParse(text, out _));
}
