using InfinityMercsApp.Services.Season;
using InfinityMercsApp.UnitTests.Support;

namespace InfinityMercsApp.UnitTests.Services.Season;

public sealed class SeasonFileStoreTests : IDisposable
{
    private readonly TempRecords _records = new();
    private readonly SeasonFileStore _store;

    public SeasonFileStoreTests() => _store = new SeasonFileStore(_records.Paths);

    public void Dispose() => _records.Dispose();

    [Fact]
    public async Task NewSeasonsAreWrittenToTheSeasonsFolder()
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
    public async Task SavedSeasonsLoadBack()
    {
        var path = await _store.CreateAsync("Iron Wolves", "company-1", "company.json");
        var season = await _store.LoadAsync(path);
        Assert.NotNull(season);
        season.CompanyName = "Steel Wolves";

        Assert.True(await _store.SaveAsync(path, season));

        Assert.Equal("Steel Wolves", (await _store.LoadAsync(path))?.CompanyName);
    }

    [Fact]
    public async Task MissingFilesLoadAsNull()
    {
        Assert.Null(await _store.LoadAsync(Path.Combine(_records.Paths.SeasonsDirectory, "missing.json")));
        Assert.Null(await _store.LoadAsync(null));
    }
}
