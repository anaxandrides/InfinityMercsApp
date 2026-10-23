using InfinityMercsApp.ApiClient.UserDocuments;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.Services.Sync;
using InfinityMercsApp.UnitTests.Support;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Services.Season;

public sealed class SeasonFileStoreTests : IDisposable
{
    private readonly TempRecords _records = new();
    private readonly ISyncOutbox _outbox = Substitute.For<ISyncOutbox>();
    private readonly SeasonFileStore _store;

    public SeasonFileStoreTests() => _store = new SeasonFileStore(_records.Paths, _outbox);

    public void Dispose() => _records.Dispose();

    [Fact]
    public async Task CreateAsync_NewSeason_WritesItToTheSeasonsFolder()
    {
        var path = await _store.CreateAsync("Iron Wolves", "company-1", "/companies/iron-wolves.json");

        Assert.Equal(_records.Paths.SeasonsDirectory, Path.GetDirectoryName(path));
        Assert.StartsWith("Iron_Wolves-", Path.GetFileName(path));
        var season = await _store.LoadAsync(path);
        Assert.NotNull(season);
        Assert.Equal("Iron Wolves", season.CompanyName);
        Assert.Equal("company-1", season.CompanyIdentifier);
        Assert.Equal("/companies/iron-wolves.json", season.CompanyFilePath);
        Assert.Empty(season.Rounds);
        Assert.NotNull(season.InitialPurchases);
    }

    [Fact]
    public async Task SaveAsync_ChangedSeason_LoadsBack()
    {
        var path = await _store.CreateAsync("Iron Wolves", "company-1", "company.json");
        var season = await _store.LoadAsync(path);
        Assert.NotNull(season);
        season.CompanyName = "Steel Wolves";

        Assert.True(await _store.SaveAsync(path, season));

        Assert.Equal("Steel Wolves", (await _store.LoadAsync(path))?.CompanyName);
    }

    [Fact]
    public async Task CreateAsync_NewSeason_GetsAnIdAndIsNotedForSync()
    {
        var path = await _store.CreateAsync("Iron Wolves", "company-1", "company.json");

        var identifier = (await _store.LoadAsync(path))?.SeasonIdentifier;
        Assert.Equal(32, identifier?.Length);
        _outbox.Received(1).RecordSave(UserDocumentKind.Season, identifier ?? string.Empty, path);
    }

    [Fact]
    public async Task SaveAsync_SeasonWithoutAnId_GivesItOne()
    {
        var path = _records.WriteSeason("old.json", """{"companyName":"Iron Wolves","companyIdentifier":"company-1"}""");
        var season = await _store.LoadAsync(path);
        Assert.NotNull(season);

        Assert.True(await _store.SaveAsync(path, season));

        Assert.False(string.IsNullOrEmpty((await _store.LoadAsync(path))?.SeasonIdentifier));
        _outbox.Received(1).RecordSave(UserDocumentKind.Season, season.SeasonIdentifier, path);
    }

    [Fact]
    public async Task LoadAsync_MissingFile_ReturnsNull()
    {
        Assert.Null(await _store.LoadAsync(Path.Combine(_records.Paths.SeasonsDirectory, "missing.json")));
        Assert.Null(await _store.LoadAsync(null));
    }
}
