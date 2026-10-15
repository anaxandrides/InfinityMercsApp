using InfinityMercsApp.Domain.Models.Army.Profiles;

namespace InfinityMercsApp.UnitTests.Stats;

public sealed class ProfileValuesTests
{
    [Theory]
    [InlineData("+1", 1)]
    [InlineData(" +0.5 ", 0.5)]
    [InlineData("1", 0)]
    [InlineData("0", 0)]
    [InlineData("-", 0)]
    [InlineData("+", 0)]
    [InlineData(null, 0)]
    public void SwcGrant_IsOnlyAPlusValue(string? swc, double expected) =>
        Assert.Equal(expected, ProfileValues.SwcGrant(swc));
}
