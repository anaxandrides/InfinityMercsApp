using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.UnitTests.Support;

namespace InfinityMercsApp.UnitTests.Services.Season;

public sealed class SeasonRecordCatalogTests : IDisposable
{
    private readonly TempRecords _records = new();

    public void Dispose() => _records.Dispose();

    [Fact]
    public async Task ListsSeasonsNewestFirstAndSkipsMalformedFiles()
    {
        _records.WriteSeason("old.json", """{"companyName":"Old","companyFilePath":"/c/old.json","createdDate":"2026-01-02"}""");
        _records.WriteSeason("new.json", """{"companyName":"New","companyFilePath":"/c/new.json","createdDate":"2026-03-04"}""");
        _records.WriteSeason("broken.json", "{not json");

        var records = await new SeasonRecordCatalog(_records.Paths).ListAsync();

        Assert.Equal(["New", "Old"], records.Select(r => r.CompanyName));
        Assert.Equal("/c/new.json", records[0].CompanyFilePath);
        Assert.Equal("Started 2026-03-04", records[0].Subtitle);
    }

    [Fact]
    public async Task FindsTheCompanyByIdentifierWhenThePathIsMissing()
    {
        _records.WriteCompany("other.json", """{"companyIdentifier":"zzz"}""");
        var company = _records.WriteCompany("mine.json", """{"companyIdentifier":"ABC"}""");
        var season = _records.WriteSeason("Unnamed-1.json", """{"companyIdentifier":"abc","createdDate":"2026-05-06"}""");

        var record = Assert.Single(await new SeasonRecordCatalog(_records.Paths).ListAsync());

        Assert.Equal(company, record.CompanyFilePath);
        Assert.Equal(season, record.SeasonFilePath);
        Assert.Equal("Unnamed-1", record.CompanyName);
    }

    [Fact]
    public async Task NoSeasonsFolderMeansNoSeasons()
    {
        var records = await new SeasonRecordCatalog(new RecordPaths(Path.Combine(_records.Paths.CompaniesDirectory, "missing"))).ListAsync();
        Assert.Empty(records);
    }
}
