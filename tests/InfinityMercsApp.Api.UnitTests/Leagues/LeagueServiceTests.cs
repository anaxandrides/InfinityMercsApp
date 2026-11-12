using InfinityMercsApp.Api.Leagues;
using InfinityMercsApp.Api.Leagues.Changes;
using InfinityMercsApp.Api.UserDocuments;
using InfinityMercsApp.ApiClient.UserDocuments;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Domain.Models.Leagues;
using InfinityMercsApp.Domain.Models.Season;
using NSubstitute;
using static InfinityMercsApp.Api.UnitTests.Leagues.Leagues;

namespace InfinityMercsApp.Api.UnitTests.Leagues;

public sealed class LeagueServiceTests
{
    private readonly InMemoryLeagueStore _store = Store();
    private readonly IUserDocumentStore _files = Substitute.For<IUserDocumentStore>();
    private readonly LeagueService _service;

    public LeagueServiceTests()
    {
        _service = new LeagueService(_store, _files, new QueuedInviteCodes("NEW234", "NEW345"), TimeProvider.System);
    }

    private void KeepSeason(string userId, string seasonId, string companyName, bool deleted = false) =>
        _files.ReadAsync<SeasonFile>(userId, UserDocumentKind.Season, seasonId, Arg.Any<CancellationToken>()).Returns(
            new StoredUserDocument<SeasonFile>(
                new UserDocument<SeasonFile>(
                    $"season-{seasonId}", userId, UserDocumentKind.Season, seasonId, 1, DateTimeOffset.UnixEpoch, deleted, null,
                    new SeasonFile { SeasonIdentifier = seasonId, CompanyName = companyName, CompanyIdentifier = "company-1" }),
                "etag"));

    [Fact]
    public async Task CreateAsync_NewLeague_MakesTheUserItsOrganizerWithAnInviteCode()
    {
        var result = await _service.CreateAsync(Organizer, new NewLeague { Name = " Spring ", DisplayName = "Olive", ArmySnapshotId = "snap-1" });

        var view = Assert.IsType<LeagueView>(result.View);
        var league = Assert.IsType<LeagueDocument>(_store[view.LeagueId]);
        Assert.Equal("Spring", league.Name);
        Assert.Equal("NEW234", league.InviteCode);
        Assert.Equal(league.LeagueId, _store.Invites["NEW234"]);
        Assert.Equal(Organizer, Assert.Single(league.Members).UserId);
        Assert.Equal(1, view.CurrentSeason);
        Assert.Equal(1, view.Season);
    }

    [Fact]
    public async Task CreateAsync_FirstCodeTaken_IssuesAnother()
    {
        _store.Invites["NEW234"] = "someone-else";

        var result = await _service.CreateAsync(Organizer, new NewLeague { Name = "Spring", DisplayName = "Olive" });

        Assert.Equal("NEW345", result.View?.InviteCode);
    }

    [Fact]
    public async Task ListAsync_SomeLeagues_ListsOnlyTheUsersByName()
    {
        await _store.TryCreateAsync(Sample() with { Id = "league-2", LeagueId = "league-2", Name = "Autumn League" });
        await _store.TryCreateAsync((Sample() with { Id = "league-3", LeagueId = "league-3", Name = "Private" }).WithMembers([]));

        var leagues = await _service.ListAsync(Member);

        Assert.Equal(["Autumn League", "Spring League"], leagues.Select(league => league.Name));
    }

    [Fact]
    public async Task GetAsync_NotAMember_IsNull()
    {

        Assert.Null(await _service.GetAsync("league-1", Outsider));
    }

    [Fact]
    public async Task ViewAsync_NoSeasonGiven_ShowsTheCurrentSeasonOnly()
    {
        _store.Keep(Sample() with { CurrentSeason = 2 });
        _store.Keep(LeagueEntry.For(LeagueId, "season-m2", "m-m", "Blue Company II", 2, 10, DateTimeOffset.UnixEpoch));
        _store.Keep(Recorded("g1", "m-m"));

        var view = await _service.ViewAsync(LeagueId, Member, null);

        Assert.Equal(2, view?.Season);
        Assert.Equal("season-m2", Assert.Single(view?.Entries ?? []).SeasonId);
        Assert.Empty(view?.Games ?? []);
    }

    [Fact]
    public async Task ViewAsync_EarlierSeason_ShowsItsEntriesAndGames()
    {
        _store.Keep(Sample() with { CurrentSeason = 2 });
        _store.Keep(Recorded("g1", "m-m"));

        var view = await _service.ViewAsync(LeagueId, Member, 1);

        Assert.Equal(1, view?.Season);
        Assert.Equal(2, view?.Entries.Count);
        Assert.Equal("g1", Assert.Single(view?.Games ?? []).GameId);
    }

    [Fact]
    public async Task ViewAsync_AnySeason_ListsEveryCompanySeasonTheUserEntered()
    {
        _store.Keep(Sample() with { CurrentSeason = 2 });
        _store.Keep(LeagueEntry.For(LeagueId, "season-m2", "m-m", "Blue Company II", 2, 10, DateTimeOffset.UnixEpoch));

        var view = await _service.ViewAsync(LeagueId, Member, 2);

        Assert.Equal([MemberSeason, "season-m2"], view?.EnteredSeasonIds.Order());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task ViewAsync_SeasonNotPlayedYet_IsNull(int season) =>
        Assert.Null(await _service.ViewAsync(LeagueId, Member, season));

    [Fact]
    public async Task ViewAsync_NotAMember_IsNull() =>
        Assert.Null(await _service.ViewAsync(LeagueId, Outsider, null));

    [Fact]
    public async Task ApplyAsync_RecordGame_KeepsTheGameAndCountsItOnBothEntries()
    {
        var result = await _service.ApplyAsync(LeagueId, Member, new RecordGameChange(Game(), "g1", DateTimeOffset.UnixEpoch));

        Assert.Equal("g1", Assert.Single(result.View?.Games ?? []).GameId);
        Assert.Equal("Supplies", _store.Game(LeagueId, "g1")?.Mission);
        Assert.Equal(1, _store.Entry(LeagueId, OrganizerSeason)?.GamesPlayed);
        Assert.Equal(1, _store.Entry(LeagueId, MemberSeason)?.GamesPlayed);
    }

    [Fact]
    public async Task ApplyAsync_GameRecordedThenRemoved_LetsTheCompanyWithdraw()
    {
        await _service.ApplyAsync(LeagueId, Member, new RecordGameChange(Game(), "g1", DateTimeOffset.UnixEpoch));
        await _service.ApplyAsync(LeagueId, Member, new RemoveGameChange("g1"));

        var result = await _service.ApplyAsync(LeagueId, Member, new WithdrawChange(MemberSeason));

        Assert.Equal(LeagueChangeStatus.Done, result.Status);
        Assert.Null(_store.Entry(LeagueId, MemberSeason));
    }

    [Fact]
    public async Task ApplyAsync_ManyGamesOverManySeasons_KeepsRecordingAndShowsOnlyTheSeasonsGames()
    {
        for (var season = 1; season <= 3; season++)
        {
            if (season > 1)
            {
                await _service.ApplyAsync(LeagueId, Organizer, new NextSeasonChange(season - 1));
                _store.Keep(LeagueEntry.For(LeagueId, $"o{season}", "m-o", "Red Company", season, 0, DateTimeOffset.UnixEpoch));
                _store.Keep(LeagueEntry.For(LeagueId, $"m{season}", "m-m", "Blue Company", season, 0, DateTimeOffset.UnixEpoch));
            }

            var winner = season == 1 ? OrganizerSeason : $"o{season}";
            var loser = season == 1 ? MemberSeason : $"m{season}";
            for (var game = 0; game < 1000; game++)
            {
                var recorded = await _service.ApplyAsync(LeagueId, Member, new RecordGameChange(Game(winner, loser), $"g{season}-{game}", DateTimeOffset.UnixEpoch));
                Assert.Equal(LeagueChangeStatus.Done, recorded.Status);
            }
        }

        var view = await _service.ViewAsync(LeagueId, Member, null);

        Assert.Equal(3000, _store.GameCount);
        Assert.Equal(1000, view?.Games.Count);
        Assert.All(view?.Games ?? [], game => Assert.Equal(3, game.LeagueSeason));
    }

    [Fact]
    public async Task ApplyAsync_SomeoneWritesFirst_WorksTheChangeOutAgain()
    {
        _store.RefuseCommits = 1;

        var result = await _service.ApplyAsync("league-1", Organizer, new ArmyChange("snap-2"));

        Assert.Equal(LeagueChangeStatus.Done, result.Status);
        Assert.Equal("snap-2", _store["league-1"]?.ArmySnapshotId);
    }

    [Fact]
    public async Task ApplyAsync_LeagueKeepsChanging_GivesUp()
    {
        _store.RefuseCommits = LeagueService.Attempts;

        var result = await _service.ApplyAsync("league-1", Organizer, new ArmyChange("snap-2"));

        Assert.Equal(LeagueChangeStatus.Conflict, result.Status);
        Assert.Equal("snap-1", _store["league-1"]?.ArmySnapshotId);
    }

    [Fact]
    public async Task ApplyAsync_NoSuchLeague_IsNotFound() =>
        Assert.Equal(LeagueChangeStatus.NotFound, (await _service.ApplyAsync("league-x", Organizer, new ArmyChange(null))).Status);

    [Fact]
    public async Task PreviewInviteAsync_CurrentCode_ShowsTheLeague()
    {

        var preview = await _service.PreviewInviteAsync("ABC234", Outsider);

        Assert.Equal("Spring League", preview?.Name);
        Assert.Equal("Olive", preview?.OrganizerName);
        Assert.False(preview?.IsMember);
    }

    [Fact]
    public async Task JoinAsync_CurrentCode_AddsTheMember()
    {

        var result = await _service.JoinAsync("ABC234", Outsider, new JoinLeague { DisplayName = "Nova" });

        Assert.Equal(LeagueChangeStatus.Done, result.Status);
        Assert.Contains(Outsider, _store["league-1"]?.MemberUserIds ?? []);
    }

    [Fact]
    public async Task JoinAsync_CodeReplaced_IsNotFound()
    {
        await _service.NewInviteAsync("league-1", Organizer);

        var result = await _service.JoinAsync("ABC234", Outsider, new JoinLeague { DisplayName = "Nova" });

        Assert.Equal(LeagueChangeStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task NewInviteAsync_Organizer_ReplacesTheCode()
    {

        var result = await _service.NewInviteAsync("league-1", Organizer);

        Assert.Equal("NEW234", result.View?.InviteCode);
        Assert.Equal(["NEW234"], _store.Invites.Keys);
    }

    [Fact]
    public async Task NewInviteAsync_Member_IsForbiddenAndIssuesNoCode()
    {

        var result = await _service.NewInviteAsync("league-1", Member);

        Assert.Equal(LeagueChangeStatus.Forbidden, result.Status);
        Assert.Equal(["ABC234"], _store.Invites.Keys);
    }

    [Fact]
    public async Task CloseInviteAsync_Organizer_ForgetsTheCode()
    {

        var result = await _service.CloseInviteAsync("league-1", Organizer);

        Assert.Equal(LeagueChangeStatus.Done, result.Status);
        Assert.Null(result.View?.InviteCode);
        Assert.Empty(_store.Invites);
    }

    [Fact]
    public async Task CloseInviteAsync_Member_KeepsTheCode()
    {

        await _service.CloseInviteAsync("league-1", Member);

        Assert.Equal(["ABC234"], _store.Invites.Keys);
    }

    [Fact]
    public async Task EnterAsync_SeasonOnline_EntersItUnderItsCompanyName()
    {
        KeepSeason(Member, "season-new", "Green Company");

        var result = await _service.EnterAsync("league-1", Member, "season-new", new LeagueEntryUpdate { Renown = 150 });

        var entry = Assert.Single(result.View?.Entries ?? [], e => e.SeasonId == "season-new");
        Assert.Equal("Green Company", entry.CompanyName);
        Assert.Equal(150, entry.Renown);
    }

    [Fact]
    public async Task EnterAsync_SeasonNotOnline_IsRejected()
    {

        var result = await _service.EnterAsync("league-1", Member, "season-new", new LeagueEntryUpdate { Renown = 150 });

        Assert.Equal(LeagueChangeStatus.Rejected, result.Status);
    }

    [Fact]
    public async Task EnterAsync_SeasonDeleted_IsRejected()
    {
        KeepSeason(Member, "season-new", "Green Company", deleted: true);

        var result = await _service.EnterAsync("league-1", Member, "season-new", new LeagueEntryUpdate { Renown = 150 });

        Assert.Equal(LeagueChangeStatus.Rejected, result.Status);
    }

    [Fact]
    public async Task EnterAsync_NotAMember_IsNotFound()
    {

        var result = await _service.EnterAsync("league-1", Outsider, "season-new", new LeagueEntryUpdate { Renown = 150 });

        Assert.Equal(LeagueChangeStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task EntryFilesAsync_Member_ReadsTheOwnersSeasonAndCompany()
    {
        KeepSeason(Organizer, OrganizerSeason, "Red Company");
        _files.ReadAsync<SavedCompanyFile>(Organizer, UserDocumentKind.Company, "company-1", Arg.Any<CancellationToken>()).Returns(
            new StoredUserDocument<SavedCompanyFile>(
                new UserDocument<SavedCompanyFile>(
                    "company-company-1", Organizer, UserDocumentKind.Company, "company-1", 1, DateTimeOffset.UnixEpoch, false, null,
                    new SavedCompanyFile { CompanyName = "Red Company", CompanyIdentifier = "company-1" }),
                "etag"));

        var entryFiles = await _service.EntryFilesAsync("league-1", Member, OrganizerSeason);

        Assert.Equal("Red Company", entryFiles?.Season?.CompanyName);
        Assert.Equal("company-1", entryFiles?.Company?.CompanyIdentifier);
    }

    [Fact]
    public async Task EntryFilesAsync_SeasonDeleted_HasNeither()
    {
        KeepSeason(Organizer, OrganizerSeason, "Red Company", deleted: true);

        var entryFiles = await _service.EntryFilesAsync("league-1", Member, OrganizerSeason);

        Assert.NotNull(entryFiles);
        Assert.Null(entryFiles.Season);
        Assert.Null(entryFiles.Company);
    }

    [Fact]
    public async Task EntryFilesAsync_NotAMember_IsNull()
    {

        Assert.Null(await _service.EntryFilesAsync("league-1", Outsider, OrganizerSeason));
    }
}
