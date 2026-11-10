using InfinityMercsApp.Api.Leagues;
using InfinityMercsApp.Api.Leagues.Changes;
using static InfinityMercsApp.Api.UnitTests.Leagues.Leagues;

namespace InfinityMercsApp.Api.UnitTests.Leagues.Changes;

/// <summary>Changes to the league document alone are written on the ETag it was read at.</summary>
public sealed class LeagueDocumentChangeTests
{
    [Fact]
    public async Task DecideAsync_LeagueChanged_ReplacesItOnItsETag()
    {
        var reads = await ReadsAsync(Store());

        var decision = await new ArmyChange("snap-2").DecideAsync(reads, Organizer);

        Assert.Equal(DocumentWriteKind.Replace, decision.Writes.League?.Kind);
        Assert.Equal(reads.ETag, decision.Writes.League?.ETag);
        Assert.Equal("snap-2", decision.Writes.League?.Document.ArmySnapshotId);
    }

    [Fact]
    public async Task DecideAsync_NothingChanged_WritesNothing()
    {
        var decision = await new JoinChange("Mo again", "m-x", DateTimeOffset.UnixEpoch).DecideAsync(await ReadsAsync(Store()), Member);

        Assert.Equal(LeagueChangeStatus.Done, decision.Status);
        Assert.True(decision.Writes.IsEmpty);
    }

    [Fact]
    public async Task DecideAsync_NextSeason_ShowsTheNewSeason() =>
        Assert.Equal(2, (await new NextSeasonChange(1).DecideAsync(await ReadsAsync(Store()), Organizer)).Season);

    [Fact]
    public async Task DecideAsync_Refused_WritesNothing()
    {
        var decision = await new ArmyChange("snap-2").DecideAsync(await ReadsAsync(Store()), Member);

        Assert.Equal(LeagueChangeStatus.Forbidden, decision.Status);
        Assert.True(decision.Writes.IsEmpty);
    }
}
