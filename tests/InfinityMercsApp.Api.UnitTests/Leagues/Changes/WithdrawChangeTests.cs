using InfinityMercsApp.Api.Leagues;
using InfinityMercsApp.Api.Leagues.Changes;
using static InfinityMercsApp.Api.UnitTests.Leagues.Leagues;

namespace InfinityMercsApp.Api.UnitTests.Leagues.Changes;

public sealed class WithdrawChangeTests
{
    [Fact]
    public async Task DecideAsync_OwnSeasonNotPlayed_DeletesItsEntry()
    {
        var decision = await new WithdrawChange(MemberSeason).DecideAsync(await ReadsAsync(Store()), Member);

        var write = Assert.Single(decision.Writes.Entries);
        Assert.Equal(DocumentWriteKind.Delete, write.Kind);
        Assert.Equal(MemberSeason, write.Document.SeasonId);
    }

    [Fact]
    public async Task DecideAsync_OrganizerTakingOutAnyone_IsDone() =>
        Assert.Equal(LeagueChangeStatus.Done, (await new WithdrawChange(MemberSeason).DecideAsync(await ReadsAsync(Store()), Organizer)).Status);

    [Fact]
    public async Task DecideAsync_SomeoneElsesSeason_IsForbidden() =>
        Assert.Equal(LeagueChangeStatus.Forbidden, (await new WithdrawChange(OrganizerSeason).DecideAsync(await ReadsAsync(Store()), Member)).Status);

    [Fact]
    public async Task DecideAsync_SeasonHasPlayed_IsRejected()
    {
        var store = Store();
        store.Keep(MemberEntry with { GamesPlayed = 1 });

        Assert.Equal(LeagueChangeStatus.Rejected, (await new WithdrawChange(MemberSeason).DecideAsync(await ReadsAsync(store), Member)).Status);
    }

    [Fact]
    public async Task DecideAsync_NotEntered_IsNotFound() =>
        Assert.Equal(LeagueChangeStatus.NotFound, (await new WithdrawChange("season-x").DecideAsync(await ReadsAsync(Store()), Member)).Status);
}
