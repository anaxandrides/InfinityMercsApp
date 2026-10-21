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
    public void SwcGrant_Swc_IsOnlyAPlusValue(string? swc, double expected) =>
        Assert.Equal(expected, ProfileValues.SwcGrant(swc));

    [Theory]
    [InlineData("0.5", true)]
    [InlineData("1", true)]
    [InlineData("+1", false)]
    [InlineData("0", false)]
    [InlineData("-", false)]
    [InlineData(null, false)]
    public void CostsSwc_Swc_IsAPositiveValueThatIsNotAGrant(string? swc, bool expected) =>
        Assert.Equal(expected, ProfileValues.CostsSwc(swc));

    [Theory]
    [InlineData("2", 2)]
    [InlineData(" 3 ", 3)]
    [InlineData("T", null)]
    [InlineData("-", null)]
    [InlineData("255", null)]
    [InlineData(null, null)]
    public void ParseAvaLimit_AvaText_ReturnsTheLimitOrNullWhenUnlimited(string? ava, int? expected) =>
        Assert.Equal(expected, ProfileValues.ParseAvaLimit(ava));
}
