using InfinityMercsApp.Api.ArmyData;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;

namespace InfinityMercsApp.Api.UnitTests.ArmyData;

public sealed class ArmySnapshotsTests
{
    private static readonly DateTimeOffset When = new(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);
    private static readonly MirroredPayload Metadata = new("m1", When);

    private static readonly ArmyMirrorManifest Snapshot = new()
    {
        Metadata = Metadata,
        Armies = [new MirroredArmy(101, "7.1", "a1", When), new MirroredArmy(201, "3.0", "b1", When)]
    };

    [Fact]
    public void IdFor_SamePayloadsInAnyOrderOrTime_IsTheSame()
    {
        var first = ArmySnapshots.IdFor(Metadata, Snapshot.Armies);
        var reordered = ArmySnapshots.IdFor(Metadata with { UpdatedAt = When.AddDays(1) }, Snapshot.Armies.Reverse());

        Assert.Equal(first, reordered);
        Assert.True(ArmySnapshots.IsId(first));
    }

    [Theory]
    [InlineData("m2", 101, "a1")]
    [InlineData("m1", 101, "a2")]
    [InlineData("m1", 301, "a1")]
    public void IdFor_AnyPayloadDifferent_IsDifferent(string metadataHash, int factionId, string armyHash)
    {
        var changed = ArmySnapshots.IdFor(
            new MirroredPayload(metadataHash, When),
            [new MirroredArmy(factionId, "7.1", armyHash, When), Snapshot.Armies[1]]);

        Assert.NotEqual(ArmySnapshots.IdFor(Metadata, Snapshot.Armies), changed);
    }

    [Theory]
    [InlineData("0123456789abcdef", true)]
    [InlineData("0123456789ABCDEF", false)]
    [InlineData("0123456789abcde", false)]
    [InlineData("latest", false)]
    [InlineData("0123456789abcdeg", false)]
    public void IsId_Text_IsSixteenLowerCaseHexDigits(string text, bool expected) => Assert.Equal(expected, ArmySnapshots.IsId(text));

    [Fact]
    public void HashOf_Request_IsThatPayloadsHash()
    {
        Assert.Equal("m1", ArmySnapshots.HashOf(Snapshot, PayloadRequest.Metadata));
        Assert.Equal("b1", ArmySnapshots.HashOf(Snapshot, PayloadRequest.Army(201)));
        Assert.Null(ArmySnapshots.HashOf(Snapshot, PayloadRequest.Army(999)));
    }
}
