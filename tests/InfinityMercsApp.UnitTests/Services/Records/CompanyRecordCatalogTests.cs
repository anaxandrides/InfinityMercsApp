using InfinityMercsApp.Infrastructure.API.UserDocuments;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Sync;
using InfinityMercsApp.UnitTests.Support;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Services.Records;

public sealed class CompanyRecordCatalogTests : IDisposable
{
    private readonly TempRecords _records = new();

    public void Dispose() => _records.Dispose();

    [Fact]
    public async Task ListAsync_SeveralFiles_ListsNewestFirstSkippingMalformed()
    {
        _records.WriteCompany("old.json", """{"companyName":"Old","companyType":"TAG Company","createdUtc":"2026-01-02T03:04:05Z"}""");
        _records.WriteCompany("new.json", """{"companyName":"New","createdUtc":"2026-03-04T05:06:07Z"}""");
        _records.WriteCompany("broken.json", "{not json");

        var records = await new CompanyRecordCatalog(_records.Paths, Substitute.For<ISyncOutbox>()).ListAsync();

        Assert.Equal(["New", "Old"], records.Select(r => r.CompanyName));
        Assert.Equal("2026-03-04 05:06:07 UTC", records[0].Subtitle);
        Assert.Equal("SVGCache/MercsIcons/noun-battle-mech-1731140.svg", records[1].CompanyIconPath);
        Assert.Equal("SVGCache/MercsIcons/noun-hack-2277937.svg", records[0].CompanyIconPath);
    }

    [Fact]
    public async Task ListAsync_UnnamedCompany_TakesItsFileName()
    {
        _records.WriteCompany("Kestrel.json", "{}");

        var record = Assert.Single(await new CompanyRecordCatalog(_records.Paths, Substitute.For<ISyncOutbox>()).ListAsync());

        Assert.Equal("Kestrel", record.CompanyName);
    }

    [Fact]
    public async Task ListAsync_SeveralSourceFactions_ShowsUpToTwoLogos()
    {
        var paths = _records.Paths with { FactionLogoDirectory = "/logos" };
        _records.WriteCompany("c.json", """{"sourceFactions":[{"factionId":101},{"factionId":101},{"factionId":0},{"factionId":102},{"factionId":103}]}""");

        var record = Assert.Single(await new CompanyRecordCatalog(paths, Substitute.For<ISyncOutbox>()).ListAsync());

        Assert.Equal([Path.Combine("/logos", "101.svg"), Path.Combine("/logos", "102.svg")], record.FactionIconPaths);
        Assert.True(record.HasSecondaryFactionIcon);
    }

    [Fact]
    public async Task ListAsync_NoSourceFactions_FallsBackToTheEntriesFactions()
    {
        _records.WriteCompany("c.json", """{"entries":[{"sourceFactionId":102},{"sourceFactionId":102}]}""");

        var record = Assert.Single(await new CompanyRecordCatalog(_records.Paths, Substitute.For<ISyncOutbox>()).ListAsync());

        Assert.Equal([_records.Paths.FactionLogoPath(102)], record.FactionIconPaths);
        Assert.False(record.HasSecondaryFactionIcon);
    }

    [Fact]
    public async Task Delete_Record_RemovesTheFile()
    {
        var path = _records.WriteCompany("c.json", "{}");
        var catalog = new CompanyRecordCatalog(_records.Paths, Substitute.For<ISyncOutbox>());

        catalog.Delete((await catalog.ListAsync())[0]);

        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task Delete_Record_NotesTheDeletionForSync()
    {
        _records.WriteCompany("c.json", """{"companyIdentifier":"c1"}""");
        var outbox = Substitute.For<ISyncOutbox>();
        var catalog = new CompanyRecordCatalog(_records.Paths, outbox);

        catalog.Delete((await catalog.ListAsync())[0]);

        outbox.Received(1).RecordDelete(UserDocumentKind.Company, "c1");
    }
}
