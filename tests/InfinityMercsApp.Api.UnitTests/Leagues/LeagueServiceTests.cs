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
    private readonly InMemoryLeagueStore _store = new();
    private readonly IUserDocumentStore _files = Substitute.For<IUserDocumentStore>();
    private readonly LeagueService _service;

    public LeagueServiceTests()
    {
        _service = new LeagueService(_store, _files, new QueuedInviteCodes("NEW234", "NEW345"), TimeProvider.System);
    }

    private async Task<LeagueDocument> KeepSampleAsync()
    {
        var league = Sample();
        await _store.TryCreateAsync(league);
        await _store.TryCreateInviteAsync(LeagueInvite.For("ABC234", league.LeagueId));
        return league;
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

        var league = Assert.IsType<LeagueDocument>(result.League);
        Assert.Equal("Spring", league.Name);
        Assert.Equal("NEW234", league.InviteCode);
        Assert.Equal(league.LeagueId, _store.Invites["NEW234"]);
        Assert.Equal(Organizer, Assert.Single(league.Members).UserId);
        Assert.Equal(1, league.CurrentSeason);
    }

    [Fact]
    public async Task CreateAsync_FirstCodeTaken_IssuesAnother()
    {
        _store.Invites["NEW234"] = "someone-else";

        var result = await _service.CreateAsync(Organizer, new NewLeague { Name = "Spring", DisplayName = "Olive" });

        Assert.Equal("NEW345", result.League?.InviteCode);
    }

    [Fact]
    public async Task ListAsync_SomeLeagues_ListsOnlyTheUsersByName()
    {
        await KeepSampleAsync();
        await _store.TryCreateAsync(Sample() with { Id = "league-2", LeagueId = "league-2", Name = "Autumn League" });
        await _store.TryCreateAsync((Sample() with { Id = "league-3", LeagueId = "league-3", Name = "Private" }).WithMembers([]));

        var leagues = await _service.ListAsync(Member);

        Assert.Equal(["Autumn League", "Spring League"], leagues.Select(league => league.Name));
    }

    [Fact]
    public async Task GetAsync_NotAMember_IsNull()
    {
        await KeepSampleAsync();

        Assert.Null(await _service.GetAsync("league-1", Outsider));
    }

    [Fact]
    public async Task ApplyAsync_SomeoneWritesFirst_WorksTheChangeOutAgain()
    {
        await KeepSampleAsync();
        _store.RefuseReplacements = 1;

        var result = await _service.ApplyAsync("league-1", Organizer, new ArmyChange("snap-2"));

        Assert.Equal(LeagueChangeStatus.Done, result.Status);
        Assert.Equal("snap-2", _store["league-1"]?.ArmySnapshotId);
    }

    [Fact]
    public async Task ApplyAsync_LeagueKeepsChanging_GivesUp()
    {
        await KeepSampleAsync();
        _store.RefuseReplacements = LeagueService.Attempts;

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
        await KeepSampleAsync();

        var preview = await _service.PreviewInviteAsync("ABC234", Outsider);

        Assert.Equal("Spring League", preview?.Name);
        Assert.Equal("Olive", preview?.OrganizerName);
        Assert.False(preview?.IsMember);
    }

    [Fact]
    public async Task JoinAsync_CurrentCode_AddsTheMember()
    {
        await KeepSampleAsync();

        var result = await _service.JoinAsync("ABC234", Outsider, new JoinLeague { DisplayName = "Nova" });

        Assert.Equal(LeagueChangeStatus.Done, result.Status);
        Assert.Contains(Outsider, _store["league-1"]?.MemberUserIds ?? []);
    }

    [Fact]
    public async Task JoinAsync_CodeReplaced_IsNotFound()
    {
        await KeepSampleAsync();
        await _service.NewInviteAsync("league-1", Organizer);

        var result = await _service.JoinAsync("ABC234", Outsider, new JoinLeague { DisplayName = "Nova" });

        Assert.Equal(LeagueChangeStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task NewInviteAsync_Organizer_ReplacesTheCode()
    {
        await KeepSampleAsync();

        var result = await _service.NewInviteAsync("league-1", Organizer);

        Assert.Equal("NEW234", result.League?.InviteCode);
        Assert.Equal(["NEW234"], _store.Invites.Keys);
    }

    [Fact]
    public async Task NewInviteAsync_Member_IsForbiddenAndIssuesNoCode()
    {
        await KeepSampleAsync();

        var result = await _service.NewInviteAsync("league-1", Member);

        Assert.Equal(LeagueChangeStatus.Forbidden, result.Status);
        Assert.Equal(["ABC234"], _store.Invites.Keys);
    }

    [Fact]
    public async Task CloseInviteAsync_Organizer_ForgetsTheCode()
    {
        await KeepSampleAsync();

        var result = await _service.CloseInviteAsync("league-1", Organizer);

        Assert.Null(result.League?.InviteCode);
        Assert.Empty(_store.Invites);
    }

    [Fact]
    public async Task CloseInviteAsync_Member_KeepsTheCode()
    {
        await KeepSampleAsync();

        await _service.CloseInviteAsync("league-1", Member);

        Assert.Equal(["ABC234"], _store.Invites.Keys);
    }

    [Fact]
    public async Task EnterAsync_SeasonOnline_EntersItUnderItsCompanyName()
    {
        await KeepSampleAsync();
        KeepSeason(Member, "season-new", "Green Company");

        var result = await _service.EnterAsync("league-1", Member, "season-new", new LeagueEntryUpdate { Renown = 150 });

        var entry = Assert.Single(result.League?.Entries ?? [], e => e.SeasonId == "season-new");
        Assert.Equal("Green Company", entry.CompanyName);
        Assert.Equal(150, entry.Renown);
    }

    [Fact]
    public async Task EnterAsync_SeasonNotOnline_IsRejected()
    {
        await KeepSampleAsync();

        var result = await _service.EnterAsync("league-1", Member, "season-new", new LeagueEntryUpdate { Renown = 150 });

        Assert.Equal(LeagueChangeStatus.Rejected, result.Status);
    }

    [Fact]
    public async Task EnterAsync_SeasonDeleted_IsRejected()
    {
        await KeepSampleAsync();
        KeepSeason(Member, "season-new", "Green Company", deleted: true);

        var result = await _service.EnterAsync("league-1", Member, "season-new", new LeagueEntryUpdate { Renown = 150 });

        Assert.Equal(LeagueChangeStatus.Rejected, result.Status);
    }

    [Fact]
    public async Task EnterAsync_NotAMember_IsNotFound()
    {
        await KeepSampleAsync();

        var result = await _service.EnterAsync("league-1", Outsider, "season-new", new LeagueEntryUpdate { Renown = 150 });

        Assert.Equal(LeagueChangeStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task EntryFilesAsync_Member_ReadsTheOwnersSeasonAndCompany()
    {
        await KeepSampleAsync();
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
        await KeepSampleAsync();
        KeepSeason(Organizer, OrganizerSeason, "Red Company", deleted: true);

        var entryFiles = await _service.EntryFilesAsync("league-1", Member, OrganizerSeason);

        Assert.NotNull(entryFiles);
        Assert.Null(entryFiles.Season);
        Assert.Null(entryFiles.Company);
    }

    [Fact]
    public async Task EntryFilesAsync_NotAMember_IsNull()
    {
        await KeepSampleAsync();

        Assert.Null(await _service.EntryFilesAsync("league-1", Outsider, OrganizerSeason));
    }
}
