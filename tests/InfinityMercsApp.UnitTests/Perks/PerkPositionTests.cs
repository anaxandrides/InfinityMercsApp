using InfinityMercsApp.Domain.Models.Perks.Tables;

namespace InfinityMercsApp.UnitTests.Perks;

public sealed class PerkPositionTests
{
    [Theory]
    [InlineData("cool-track-4-tier-2", 4, 2)]
    [InlineData("BODY-TRACK-10-TIER-1", 10, 1)]
    public void ReadsTrackAndTierFromTheNodeId(string nodeId, int track, int tier) =>
        Assert.Equal(new PerkPosition(track, tier), PerkPosition.Parse(nodeId));

    [Theory]
    [InlineData("cool-root")]
    [InlineData("")]
    [InlineData(null)]
    public void IdsWithoutAPositionGiveNone(string? nodeId) => Assert.Null(PerkPosition.Parse(nodeId));
}
