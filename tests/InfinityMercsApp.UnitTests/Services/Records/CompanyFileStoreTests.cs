using InfinityMercsApp.ApiClient.UserDocuments;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Sync;
using InfinityMercsApp.UnitTests.Services.Sync;
using InfinityMercsApp.UnitTests.Support;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Services.Records;

public sealed class CompanyFileStoreTests : IDisposable
{
    private readonly TempRecords _records = new();

    public void Dispose() => _records.Dispose();

    [Fact]
    public async Task SaveAsync_AnyCompany_IsNotedForSync()
    {
        var outbox = Substitute.For<ISyncOutbox>();
        var path = Path.Combine(_records.Paths.CompaniesDirectory, "iron-wolves-0001.json");

        await new CompanyFileStore(outbox).SaveAsync(path, SyncTestFiles.Company("c1"));

        Assert.True(File.Exists(path));
        outbox.Received(1).RecordSave(UserDocumentKind.Company, "c1", path);
    }
}
