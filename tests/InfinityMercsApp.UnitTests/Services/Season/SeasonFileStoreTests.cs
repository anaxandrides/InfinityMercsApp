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

    [Fact]
    public async Task LoadAsync_Unreadable_IsNull()
    {
        var path = Path.Combine(_records.Paths.SeasonsDirectory, "broken.json");
        Directory.CreateDirectory(_records.Paths.SeasonsDirectory);
        await File.WriteAllTextAsync(path, "{ broken");

        Assert.Null(await _store.LoadAsync(path));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("missing.json")]
    public async Task LoadAsync_NoFile_IsNull(string? path) => Assert.Null(await _store.LoadAsync(path));

    [Fact]
    public async Task SaveAsync_NoPathOrSeason_IsFalse()
    {
        Assert.False(await _store.SaveAsync(" ", new Domain.Models.Season.SeasonFile()));
        Assert.False(await _store.SaveAsync("season.json", null));
    }

    [Fact]
    public async Task SaveAsync_FolderMissing_IsFalse() =>
        Assert.False(await _store.SaveAsync(Path.Combine(_records.Paths.SeasonsDirectory, "missing", "season.json"), new Domain.Models.Season.SeasonFile()));

    [Fact]
    public async Task UpdateAsync_Season_SavesTheChange()
    {
        var path = await _store.CreateAsync("Iron Wolves", "company-1", "company.json");

        Assert.True(await _store.UpdateAsync(path, season => season.CompanyName = "Steel Wolves"));

        Assert.Equal("Steel Wolves", (await _store.LoadAsync(path))?.CompanyName);
    }

    [Fact]
    public async Task UpdateAsync_NoSeason_IsFalse() => Assert.False(await _store.UpdateAsync("missing.json", _ => { }));

    [Fact]
    public async Task UpdateLatestRoundAsync_Rounds_ChangesOnlyTheLatest()
    {
        var path = await _store.CreateAsync("Iron Wolves", "company-1", "company.json");
        await _store.UpdateAsync(path, season => season.Rounds = [new() { RoundIndex = 1 }, new() { RoundIndex = 2 }]);

        Assert.True(await _store.UpdateLatestRoundAsync(path, round => round.TemporaryStore = "kept"));

        var rounds = (await _store.LoadAsync(path))?.Rounds ?? [];
        Assert.Equal([string.Empty, "kept"], rounds.Select(round => round.TemporaryStore));
    }

    [Fact]
    public async Task UpdateLatestRoundAsync_NoRounds_IsFalse()
    {
        var path = await _store.CreateAsync("Iron Wolves", "company-1", "company.json");

        Assert.False(await _store.UpdateLatestRoundAsync(path, round => round.TemporaryStore = "lost"));
    }
}
