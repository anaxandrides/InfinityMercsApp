using InfinityMercsApp.Api.UserDocuments;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Infrastructure.API.UserDocuments;
using NSubstitute;

namespace InfinityMercsApp.Api.UnitTests.UserDocuments;

public sealed class UserDocumentServiceTests
{
    private const string UserId = "user-1";
    private const string DocumentId = "c0ffee";
    private const UserDocumentKind Kind = UserDocumentKind.Company;

    private static readonly DateTimeOffset Earlier = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly IUserDocumentStore _store = Substitute.For<IUserDocumentStore>();
    private readonly TimeProvider _time = Substitute.For<TimeProvider>();
    private readonly UserDocumentService _service;

    public UserDocumentServiceTests()
    {
        _time.GetUtcNow().Returns(Now);
        _store.TryCreateAsync(Arg.Any<UserDocument<SavedCompanyFile>>(), Arg.Any<CancellationToken>()).Returns(true);
        _store.TryReplaceAsync(Arg.Any<UserDocument<SavedCompanyFile>>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _service = new UserDocumentService(_store, _time);
    }

    private static SavedCompanyFile Company(string name) => new() { CompanyName = name };

    private static UserDocument<SavedCompanyFile> Stored(int revision, bool deleted = false) => new(
        UserDocumentIds.For(Kind, DocumentId),
        UserId,
        Kind,
        DocumentId,
        revision,
        Earlier,
        deleted,
        "0123456789abcdef",
        deleted ? null : Company("Blue Team"));

    private void Store(UserDocument<SavedCompanyFile> document) =>
        _store.ReadAsync<SavedCompanyFile>(UserId, Kind, DocumentId, Arg.Any<CancellationToken>())
            .Returns(new StoredUserDocument<SavedCompanyFile>(document, "etag-1"));

    private Task<SaveOutcome<SavedCompanyFile>> SaveAsync(int baseRevision) =>
        _service.SaveAsync(UserId, Kind, DocumentId, new UserDocumentWrite<SavedCompanyFile>
        {
            BaseRevision = baseRevision,
            ArmySnapshotId = "fedcba9876543210",
            Content = Company("Red Team")
        });

    private Task<SaveOutcome<SavedCompanyFile>> DeleteAsync(int baseRevision) =>
        _service.DeleteAsync<SavedCompanyFile>(UserId, Kind, DocumentId, baseRevision);

    [Fact]
    public async Task SaveAsync_NewFile_CreatesRevisionOne()
    {
        var outcome = await SaveAsync(0);

        Assert.Equal(SaveResult.Saved, outcome.Result);
        Assert.Equal(1, outcome.Current?.Revision);
        Assert.Equal(Now, outcome.Current?.UpdatedAt);
        Assert.Equal("Red Team", outcome.Current?.Content?.CompanyName);
        await _store.Received(1).TryCreateAsync(
            Arg.Is<UserDocument<SavedCompanyFile>>(document => document.Id == "company-c0ffee" && document.UserId == UserId &&
                                                               document.ArmySnapshotId == "fedcba9876543210" && !document.Deleted),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_BuildsOnTheCurrentRevision_ReplacesItAsTheNextOne()
    {
        Store(Stored(3));

        var outcome = await SaveAsync(3);

        Assert.Equal(SaveResult.Saved, outcome.Result);
        Assert.Equal(4, outcome.Current?.Revision);
        await _store.Received(1).TryReplaceAsync(
            Arg.Is<UserDocument<SavedCompanyFile>>(document => document.Revision == 4), "etag-1", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(2)]
    [InlineData(0)]
    [InlineData(4)]
    public async Task SaveAsync_BuildsOnAnotherRevision_IsAConflictWithTheCurrentFile(int baseRevision)
    {
        Store(Stored(3));

        var outcome = await SaveAsync(baseRevision);

        Assert.Equal(SaveResult.Conflict, outcome.Result);
        Assert.Equal(3, outcome.Current?.Revision);
        Assert.Equal("Blue Team", outcome.Current?.Content?.CompanyName);
        await _store.DidNotReceive().TryReplaceAsync(
            Arg.Any<UserDocument<SavedCompanyFile>>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_UpdateOfAFileThatIsNotThere_IsAConflict()
    {
        var outcome = await SaveAsync(2);

        Assert.Equal(SaveResult.Conflict, outcome.Result);
        Assert.Null(outcome.Current);
    }

    [Fact]
    public async Task SaveAsync_AnotherDeviceSavedInBetween_IsAConflict()
    {
        Store(Stored(3));
        _store.TryReplaceAsync(Arg.Any<UserDocument<SavedCompanyFile>>(), "etag-1", Arg.Any<CancellationToken>()).Returns(false);

        var outcome = await SaveAsync(3);

        Assert.Equal(SaveResult.Conflict, outcome.Result);
    }

    [Fact]
    public async Task SaveAsync_DeletedFile_CanBeSavedAgainOnItsRevision()
    {
        Store(Stored(5, deleted: true));

        var outcome = await SaveAsync(5);

        Assert.Equal(SaveResult.Saved, outcome.Result);
        Assert.False(outcome.Current?.Deleted);
    }

    [Fact]
    public async Task DeleteAsync_CurrentRevision_KeepsAnEmptyDeletedRevision()
    {
        Store(Stored(3));

        var outcome = await DeleteAsync(3);

        Assert.Equal(SaveResult.Saved, outcome.Result);
        await _store.Received(1).TryReplaceAsync(
            Arg.Is<UserDocument<SavedCompanyFile>>(document => document.Deleted && document.Content == null && document.Revision == 4),
            "etag-1",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_StaleRevision_IsAConflict()
    {
        Store(Stored(3));

        Assert.Equal(SaveResult.Conflict, (await DeleteAsync(2)).Result);
    }

    [Fact]
    public async Task DeleteAsync_MissingOrAlreadyDeleted_IsNotFound()
    {
        Assert.Equal(SaveResult.NotFound, (await DeleteAsync(0)).Result);

        Store(Stored(4, deleted: true));

        Assert.Equal(SaveResult.NotFound, (await DeleteAsync(4)).Result);
    }

    [Fact]
    public async Task ListAsync_Files_AreListedWithoutTheirContent()
    {
        _store.ListAsync<SavedCompanyFile>(UserId, Kind, Arg.Any<CancellationToken>()).Returns([Stored(2)]);

        var listed = Assert.Single(await _service.ListAsync<SavedCompanyFile>(UserId, Kind));

        Assert.Equal(DocumentId, listed.DocumentId);
        Assert.Equal(2, listed.Revision);
        Assert.Null(listed.Content);
    }

    [Fact]
    public async Task GetAsync_File_IsReturnedWithItsContent()
    {
        Store(Stored(2));

        var document = await _service.GetAsync<SavedCompanyFile>(UserId, Kind, DocumentId);

        Assert.Equal("Blue Team", document?.Content?.CompanyName);
    }
}
