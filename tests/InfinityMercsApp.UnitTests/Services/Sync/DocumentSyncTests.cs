using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Infrastructure.API.UserDocuments;
using InfinityMercsApp.Services.SignIn;
using InfinityMercsApp.Services.Sync;
using InfinityMercsApp.UnitTests.Support;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Services.Sync;

public sealed class DocumentSyncTests : IDisposable
{
    private const string Token = "token-1";
    private static readonly DocumentKey Wolves = new(UserDocumentKind.Company, "c1");

    private readonly TempRecords _records = new();
    private readonly IUserDocumentsApi _api = Substitute.For<IUserDocumentsApi>();
    private readonly ISignInService _signIn = Substitute.For<ISignInService>();
    private readonly SyncLedger _ledger;
    private readonly DocumentSync _sync;

    public DocumentSyncTests()
    {
        _ledger = new SyncLedger(_records.Paths);
        _sync = new DocumentSync(_ledger, _api, _signIn, _records.Paths);
        _signIn.GetAccountAsync(Arg.Any<CancellationToken>()).Returns(new SignedInAccount("kestrel@example.com"));
        _signIn.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns(Token);
        Online<SavedCompanyFile>(UserDocumentKind.Company);
        Online<SeasonFile>(UserDocumentKind.Season);
    }

    public void Dispose() => _records.Dispose();

    private string WolvesPath => Path.Combine(_records.Paths.CompaniesDirectory, "iron-wolves-0001.json");

    [Fact]
    public async Task RunAsync_SignedOut_DoesNothing()
    {
        _signIn.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns((string?)null);

        var report = await _sync.RunAsync();

        Assert.Equal(SyncRunStatus.SignedOut, report.Status);
        await _api.DidNotReceiveWithAnyArgs().ListAsync<SavedCompanyFile>(default, default!, default);
    }

    [Fact]
    public async Task RunAsync_CompanyOnlyHere_SendsItFromRevisionZero()
    {
        WriteWolves();
        Saves(new UserDocumentsReply<UserDocumentView<SavedCompanyFile>>(UserDocumentsStatus.Ok, View(revision: 1)));

        var report = await _sync.RunAsync();

        Assert.Equal(1, report.Sent);
        await _api.Received(1).SaveAsync(
            UserDocumentKind.Company, "c1", Arg.Is<UserDocumentWrite<SavedCompanyFile>>(write => write.BaseRevision == 0 && write.Content.CompanyName == "Iron Wolves"), Token, Arg.Any<CancellationToken>());
        Assert.Equal(SyncOperation.None, _ledger.Find(Wolves)?.Pending);
        Assert.Equal(1, _ledger.Find(Wolves)?.Revision);
    }

    [Fact]
    public async Task RunAsync_CompanyBreakingTheRules_IsNotSent()
    {
        LocalRecordFiles.Write(WolvesPath, new SavedCompanyFile
        {
            CompanyName = "Iron Wolves",
            CompanyType = "Standard Company - Vanilla",
            CompanyIdentifier = "c1",
            SourceFactions = [new SavedCompanyFaction { FactionId = 101 }]
        });

        await _sync.RunAsync();

        await _api.DidNotReceiveWithAnyArgs().SaveAsync<SavedCompanyFile>(default, default!, default!, default!, default);
        Assert.Equal(SyncOperation.None, _ledger.Find(Wolves)?.Pending);
    }

    [Fact]
    public async Task RunAsync_SeasonWithoutAnId_IsGivenOneAndSent()
    {
        var path = Path.Combine(_records.Paths.SeasonsDirectory, "old.json");
        LocalRecordFiles.Write(path, SyncTestFiles.Season(string.Empty, "c1"));
        _api.SaveAsync(UserDocumentKind.Season, Arg.Any<string>(), Arg.Any<UserDocumentWrite<SeasonFile>>(), Token, Arg.Any<CancellationToken>())
            .Returns(new UserDocumentsReply<UserDocumentView<SeasonFile>>(UserDocumentsStatus.Ok, new UserDocumentView<SeasonFile> { Revision = 1 }));

        await _sync.RunAsync();

        var identifier = LocalRecordFiles.Read<SeasonFile>(path)?.SeasonIdentifier;
        Assert.False(string.IsNullOrEmpty(identifier));
        await _api.Received(1).SaveAsync(UserDocumentKind.Season, identifier, Arg.Any<UserDocumentWrite<SeasonFile>>(), Token, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ConflictWithTheSameVersion_TakesItsRevision()
    {
        WriteWolves();
        Saves(new UserDocumentsReply<UserDocumentView<SavedCompanyFile>>(UserDocumentsStatus.Conflict, View(revision: 4)));

        var report = await _sync.RunAsync();

        Assert.Empty(report.Conflicts);
        Assert.Equal(4, _ledger.Find(Wolves)?.Revision);
        Assert.Equal(SyncOperation.None, _ledger.Find(Wolves)?.Pending);
    }

    [Fact]
    public async Task RunAsync_ConflictWithAnotherVersion_HoldsItForTheUser()
    {
        WriteWolves();
        Saves(new UserDocumentsReply<UserDocumentView<SavedCompanyFile>>(UserDocumentsStatus.Conflict, View(revision: 4, name: "Steel Wolves")));

        var report = await _sync.RunAsync();

        var conflict = Assert.Single(report.Conflicts);
        Assert.Equal("Iron Wolves", conflict.Name);
        Assert.Equal(4, conflict.OtherRevision);
        Assert.False(conflict.DeletedHere);
        Assert.Equal("Iron Wolves", LocalRecordFiles.Read<SavedCompanyFile>(WolvesPath)?.CompanyName);
    }

    [Fact]
    public async Task RunAsync_Offline_KeepsTheOutbox()
    {
        WriteWolves();
        Saves(UserDocumentsReply<UserDocumentView<SavedCompanyFile>>.Of(UserDocumentsStatus.Unavailable));

        var report = await _sync.RunAsync();

        Assert.Equal(SyncRunStatus.Offline, report.Status);
        Assert.Equal(SyncOperation.Save, _ledger.Find(Wolves)?.Pending);
    }

    [Fact]
    public async Task RunAsync_DeletedHere_DeletesItOnline()
    {
        _ledger.Received(Wolves, WolvesPath, revision: 3);
        _ledger.RecordDelete(UserDocumentKind.Company, "c1");
        _api.DeleteAsync<SavedCompanyFile>(UserDocumentKind.Company, "c1", 3, Token, Arg.Any<CancellationToken>())
            .Returns(new UserDocumentsReply<UserDocumentView<SavedCompanyFile>>(UserDocumentsStatus.Ok, View(revision: 4, deleted: true)));

        await _sync.RunAsync();

        Assert.Null(_ledger.Find(Wolves));
    }

    [Fact]
    public async Task RunAsync_NewOnAnotherDevice_FetchesIt()
    {
        Online<SavedCompanyFile>(UserDocumentKind.Company, View(revision: 2) with { Content = null });
        _api.GetAsync<SavedCompanyFile>(UserDocumentKind.Company, "c1", Token, Arg.Any<CancellationToken>())
            .Returns(new UserDocumentsReply<UserDocumentView<SavedCompanyFile>>(UserDocumentsStatus.Ok, View(revision: 2)));

        var report = await _sync.RunAsync();

        Assert.Equal(1, report.Received);
        var document = _ledger.Find(Wolves);
        Assert.Equal(2, document?.Revision);
        Assert.Equal("iron-wolves-0001.json", document?.RelativePath);
        Assert.Equal("Iron Wolves", LocalRecordFiles.Read<SavedCompanyFile>(WolvesPath)?.CompanyName);
    }

    [Fact]
    public async Task RunAsync_HeldForTheUser_IsNotFetched()
    {
        WriteWolves();
        _ledger.Received(Wolves, WolvesPath, revision: 1);
        _ledger.RecordSave(UserDocumentKind.Company, "c1", WolvesPath);
        Saves(new UserDocumentsReply<UserDocumentView<SavedCompanyFile>>(UserDocumentsStatus.Conflict, View(revision: 2, name: "Steel Wolves")));
        Online<SavedCompanyFile>(UserDocumentKind.Company, View(revision: 2) with { Content = null });

        await _sync.RunAsync();

        await _api.DidNotReceive().GetAsync<SavedCompanyFile>(UserDocumentKind.Company, "c1", Token, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_DeletedOnAnotherDevice_DeletesItHere()
    {
        WriteWolves();
        _ledger.Received(Wolves, WolvesPath, revision: 1);
        Online<SavedCompanyFile>(UserDocumentKind.Company, View(revision: 2, deleted: true));

        await _sync.RunAsync();

        Assert.False(File.Exists(WolvesPath));
        Assert.Null(_ledger.Find(Wolves));
    }

    [Fact]
    public async Task ResolveAsync_KeepThisDevice_SendsItOverTheOtherVersion()
    {
        WriteWolves();
        _ledger.Received(Wolves, WolvesPath, revision: 1);
        var conflict = new SyncConflict { Kind = UserDocumentKind.Company, DocumentId = "c1", OtherRevision = 4 };
        _ledger.AddConflict(conflict);

        Assert.True(await _sync.ResolveAsync(conflict, SyncChoice.KeepThisDevice));

        Assert.Empty(_ledger.Conflicts);
        Assert.Equal(4, _ledger.Find(Wolves)?.Revision);
        Assert.Equal(SyncOperation.Save, _ledger.Find(Wolves)?.Pending);
    }

    [Fact]
    public async Task ResolveAsync_UseOtherDevice_WritesTheOtherVersionHere()
    {
        WriteWolves();
        _ledger.Received(Wolves, WolvesPath, revision: 1);
        var conflict = new SyncConflict { Kind = UserDocumentKind.Company, DocumentId = "c1", OtherRevision = 4 };
        _ledger.AddConflict(conflict);
        _api.GetAsync<SavedCompanyFile>(UserDocumentKind.Company, "c1", Token, Arg.Any<CancellationToken>())
            .Returns(new UserDocumentsReply<UserDocumentView<SavedCompanyFile>>(UserDocumentsStatus.Ok, View(revision: 4, name: "Steel Wolves")));

        Assert.True(await _sync.ResolveAsync(conflict, SyncChoice.UseOtherDevice));

        Assert.Empty(_ledger.Conflicts);
        Assert.Equal("Steel Wolves", LocalRecordFiles.Read<SavedCompanyFile>(WolvesPath)?.CompanyName);
        Assert.Equal(SyncOperation.None, _ledger.Find(Wolves)?.Pending);
        Assert.Equal(4, _ledger.Find(Wolves)?.Revision);
    }

    [Fact]
    public async Task ResolveAsync_UseOtherDeviceThatDeletedIt_DeletesItHere()
    {
        WriteWolves();
        _ledger.Received(Wolves, WolvesPath, revision: 1);
        var conflict = new SyncConflict { Kind = UserDocumentKind.Company, DocumentId = "c1", OtherRevision = 4, OtherDeleted = true };
        _ledger.AddConflict(conflict);

        Assert.True(await _sync.ResolveAsync(conflict, SyncChoice.UseOtherDevice));

        Assert.False(File.Exists(WolvesPath));
        Assert.Null(_ledger.Find(Wolves));
    }

    private void WriteWolves() => LocalRecordFiles.Write(WolvesPath, SyncTestFiles.Company("c1"));

    private void Saves(UserDocumentsReply<UserDocumentView<SavedCompanyFile>> reply) =>
        _api.SaveAsync(UserDocumentKind.Company, "c1", Arg.Any<UserDocumentWrite<SavedCompanyFile>>(), Token, Arg.Any<CancellationToken>()).Returns(reply);

    private void Online<TContent>(UserDocumentKind kind, params IEnumerable<UserDocumentView<TContent>> views)
        where TContent : class =>
        _api.ListAsync<TContent>(kind, Token, Arg.Any<CancellationToken>())
            .Returns(new UserDocumentsReply<IReadOnlyList<UserDocumentView<TContent>>>(UserDocumentsStatus.Ok, views.ToList()));

    private static UserDocumentView<SavedCompanyFile> View(int revision, string name = "Iron Wolves", bool deleted = false) => new()
    {
        DocumentId = "c1",
        Revision = revision,
        UpdatedAt = new DateTimeOffset(2026, 10, 21, 18, 0, 0, TimeSpan.Zero),
        Deleted = deleted,
        Content = deleted ? null : SyncTestFiles.Company("c1", name)
    };
}
