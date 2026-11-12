using InfinityMercsApp.Api.Leagues;
using InfinityMercsApp.Api.Leagues.Changes;
using static InfinityMercsApp.Api.UnitTests.Leagues.Leagues;

namespace InfinityMercsApp.Api.UnitTests.Leagues.Changes;

public sealed class RemoveGameChangeTests
{
    private static InMemoryLeagueStore StoreWithGame(string recordedBy)
    {
        var store = Store();
        store.Keep(OrganizerEntry with { GamesPlayed = 1 });
        store.Keep(MemberEntry with { GamesPlayed = 1 });
        store.Keep(Recorded("g1", recordedBy));
        return store;
    }

    [Theory]
    [InlineData(Member)]
    [InlineData(Organizer)]
    public async Task DecideAsync_RecorderOrOrganizer_DeletesIt(string userId)
    {
        var decision = await new RemoveGameChange("g1").DecideAsync(await ReadsAsync(StoreWithGame("m-m")), userId);

        Assert.Equal(DocumentWriteKind.Delete, Assert.Single(decision.Writes.Games).Kind);
    }

    [Fact]
    public async Task DecideAsync_Recorder_StopsEachCompanysEntryCountingIt()
    {
        var decision = await new RemoveGameChange("g1").DecideAsync(await ReadsAsync(StoreWithGame("m-m")), Member);

        Assert.Equal(2, decision.Writes.Entries.Count);
        Assert.All(decision.Writes.Entries, write => Assert.Equal(0, write.Document.GamesPlayed));
    }

    [Fact]
    public async Task DecideAsync_SomeoneElsesGame_IsForbidden() =>
        Assert.Equal(LeagueChangeStatus.Forbidden, (await new RemoveGameChange("g1").DecideAsync(await ReadsAsync(StoreWithGame("m-o")), Member)).Status);

    [Fact]
    public async Task DecideAsync_NoSuchGame_IsNotFound() =>
        Assert.Equal(LeagueChangeStatus.NotFound, (await new RemoveGameChange("g1").DecideAsync(await ReadsAsync(Store()), Member)).Status);
}
