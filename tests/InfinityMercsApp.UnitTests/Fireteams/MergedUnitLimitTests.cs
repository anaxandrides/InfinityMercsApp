using InfinityMercsApp.Domain.Fireteams;

namespace InfinityMercsApp.UnitTests.Fireteams;

public sealed class MergedUnitLimitTests
{
    [Fact]
    public void MergingWidensTheRangeAndKeepsTheFirstSlug() =>
        Assert.Equal(
            new MergedUnitLimit(0, 4, "orc", true),
            new MergedUnitLimit(1, 2, "orc", false).MergeWith(new MergedUnitLimit(0, 4, "orc-troops", true)));

    [Fact]
    public void AMissingSlugIsTakenFromTheOtherChart() =>
        Assert.Equal("orc", new MergedUnitLimit(1, 2, " ", false).MergeWith(new MergedUnitLimit(1, 2, "orc", false)).Slug);

    [Fact]
    public void AMandatoryMinimumShowsAsAnAsterisk()
    {
        Assert.Equal("*", new MergedUnitLimit(0, 3, null, true).MinText);
        Assert.Equal("1", new MergedUnitLimit(1, 3, null, false).MinText);
        Assert.Equal("3", new MergedUnitLimit(1, 3, null, false).MaxText);
    }
}
