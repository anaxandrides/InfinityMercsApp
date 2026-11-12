using InfinityMercsApp.Api.Leagues;
using InfinityMercsApp.Api.Leagues.Changes;
using static InfinityMercsApp.Api.UnitTests.Leagues.Leagues;

namespace InfinityMercsApp.Api.UnitTests.Leagues.Changes;

public sealed class RecordGameChangeTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task DecideAsync_PlayerInTheGame_CreatesTheGameForTheCurrentSeason()
    {
        var decision = await new RecordGameChange(Game(), "g1", Now).DecideAsync(await ReadsAsync(Store()), Member);

        var write = Assert.Single(decision.Writes.Games);
        Assert.Equal(DocumentWriteKind.Create, write.Kind);
        Assert.Equal("m-m", write.Document.RecordedBy);
        Assert.Equal(1, write.Document.LeagueSeason);
        Assert.Equal(7, write.Document.Sides[0].ObjectivePoints);
    }

    [Fact]
    public async Task DecideAsync_PlayerInTheGame_CountsItOnEachCompanysEntry()
    {
        var store = Store();
        var etag = (await store.ReadEntryAsync(LeagueId, MemberSeason))?.ETag;

        var decision = await new RecordGameChange(Game(), "g1", Now).DecideAsync(await ReadsAsync(store), Member);

        Assert.All(decision.Writes.Entries, write => Assert.Equal(1, write.Document.GamesPlayed));
        Assert.Equal(etag, Assert.Single(decision.Writes.Entries, write => write.Document.SeasonId == MemberSeason).ETag);
        Assert.Equal(2, decision.Writes.Entries.Count);
    }

    [Fact]
    public async Task DecideAsync_MemberNotInTheGame_IsRejected()
    {
        var store = Store(Sample().WithMembers([.. Sample().Members, new LeagueMember("m-n", Outsider, "Nova", Now)]));

        Assert.Equal(LeagueChangeStatus.Rejected, (await new RecordGameChange(Game(), "g1", Now).DecideAsync(await ReadsAsync(store), Outsider)).Status);
    }

    [Fact]
    public async Task DecideAsync_CompanyNotEntered_IsRejected() =>
        Assert.Equal(LeagueChangeStatus.Rejected, (await new RecordGameChange(Game(loser: "season-x"), "g1", Now).DecideAsync(await ReadsAsync(Store()), Member)).Status);

    [Fact]
    public async Task DecideAsync_CompanyFromAnEarlierSeason_IsRejected() =>
        Assert.Equal(
            LeagueChangeStatus.Rejected,
            (await new RecordGameChange(Game(), "g1", Now).DecideAsync(await ReadsAsync(Store(Sample() with { CurrentSeason = 2 })), Member)).Status);

    [Fact]
    public async Task DecideAsync_NotAMember_IsNotFound() =>
        Assert.Equal(LeagueChangeStatus.NotFound, (await new RecordGameChange(Game(), "g1", Now).DecideAsync(await ReadsAsync(Store()), Outsider)).Status);
}
