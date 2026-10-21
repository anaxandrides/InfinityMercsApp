using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.UnitTests.Stats;

public sealed class UnitTypeCodesTests
{
    [Theory]
    [InlineData("LI - Garrison Troops", "LI")]
    [InlineData("tag – Mercenary", "TAG")]
    [InlineData("  ", "")]
    [InlineData(null, "")]
    public void FromSubtitle_Subtitle_ReturnsTheLeadingTroopType(string? subtitle, string expected) =>
        Assert.Equal(expected, UnitTypeCodes.FromSubtitle(subtitle));
}
