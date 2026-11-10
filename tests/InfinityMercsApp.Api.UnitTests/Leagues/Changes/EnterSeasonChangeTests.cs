using InfinityMercsApp.Api.Leagues;
using InfinityMercsApp.Api.Leagues.Changes;
using static InfinityMercsApp.Api.UnitTests.Leagues.Leagues;

namespace InfinityMercsApp.Api.UnitTests.Leagues.Changes;

public sealed class EnterSeasonChangeTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task DecideAsync_NewSeason_CreatesItsEntryInTheCurrentLeagueSeason()
    {
        var reads = await ReadsAsync(Store(Sample() with { CurrentSeason = 2 }));

        var decision = await new EnterSeasonChange("season-new", "Green Company", 210, Now).DecideAsync(reads, Member);

        var write = Assert.Single(decision.Writes.Entries);
        Assert.Equal(DocumentWriteKind.Create, write.Kind);
        Assert.Equal("m-m", write.Document.MemberId);
        Assert.Equal(2, write.Document.LeagueSeason);
        Assert.Equal(210, write.Document.Renown);
        Assert.Equal(2, decision.Season);
    }

    [Fact]
    public async Task DecideAsync_OwnSeasonEntered_ReplacesItsRenownAndNameOnItsETag()
    {
        var store = Store();
        var etag = (await store.ReadEntryAsync(LeagueId, MemberSeason))?.ETag;

        var decision = await new EnterSeasonChange(MemberSeason, "Blue Company II", 280, Now).DecideAsync(await ReadsAsync(store), Member);

        var write = Assert.Single(decision.Writes.Entries);
        Assert.Equal(DocumentWriteKind.Replace, write.Kind);
        Assert.Equal(etag, write.ETag);
        Assert.Equal(280, write.Document.Renown);
        Assert.Equal("Blue Company II", write.Document.CompanyName);
        Assert.Equal(Now, write.Document.UpdatedAt);
    }

    [Fact]
    public async Task DecideAsync_SomeoneElsesSeason_IsForbidden() =>
        Assert.Equal(LeagueChangeStatus.Forbidden, (await new EnterSeasonChange(OrganizerSeason, "Mine now", 1, Now).DecideAsync(await ReadsAsync(Store()), Member)).Status);

    [Fact]
    public async Task DecideAsync_NotAMember_IsNotFound() =>
        Assert.Equal(LeagueChangeStatus.NotFound, (await new EnterSeasonChange("season-x", "X", 1, Now).DecideAsync(await ReadsAsync(Store()), Outsider)).Status);
}
