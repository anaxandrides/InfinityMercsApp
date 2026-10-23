using InfinityMercsApp.ApiClient.UserDocuments;
using InfinityMercsApp.Services.Sync;
using InfinityMercsApp.UnitTests.Support;

namespace InfinityMercsApp.UnitTests.Services.Sync;

public sealed class SyncLedgerTests : IDisposable
{
    private static readonly DocumentKey Wolves = new(UserDocumentKind.Company, "c1");

    private readonly TempRecords _records = new();

    public void Dispose() => _records.Dispose();

    private string CompanyPath => Path.Combine(_records.Paths.CompaniesDirectory, "wolves-0001.json");

    [Fact]
    public void RecordSave_NewFile_WaitsToBeSentFromRevisionZero()
    {
        var ledger = new SyncLedger(_records.Paths);

        ledger.RecordSave(UserDocumentKind.Company, "c1", CompanyPath);

        var document = Assert.Single(ledger.Documents);
        Assert.Equal(SyncOperation.Save, document.Pending);
        Assert.Equal(0, document.Revision);
        Assert.Equal("wolves-0001.json", document.RelativePath);
    }

    [Fact]
    public void RecordSave_AnyFile_IsKeptAcrossLaunches()
    {
        new SyncLedger(_records.Paths).RecordSave(UserDocumentKind.Company, "c1", CompanyPath);

        var reopened = new SyncLedger(_records.Paths);

        Assert.Equal(SyncOperation.Save, reopened.Find(Wolves)?.Pending);
    }

    [Fact]
    public void RecordDelete_NeverSent_IsForgotten()
    {
        var ledger = new SyncLedger(_records.Paths);
        ledger.RecordSave(UserDocumentKind.Company, "c1", CompanyPath);

        ledger.RecordDelete(UserDocumentKind.Company, "c1");

        Assert.Empty(ledger.Documents);
    }

    [Fact]
    public void RecordDelete_SentBefore_WaitsToDeleteOnline()
    {
        var ledger = new SyncLedger(_records.Paths);
        ledger.RecordSave(UserDocumentKind.Company, "c1", CompanyPath);
        ledger.Sent(Wolves, change: 1, revision: 1);

        ledger.RecordDelete(UserDocumentKind.Company, "c1");

        Assert.Equal(SyncOperation.Delete, ledger.Find(Wolves)?.Pending);
    }

    [Fact]
    public void Sent_SavedAgainMeanwhile_StillWaits()
    {
        var ledger = new SyncLedger(_records.Paths);
        ledger.RecordSave(UserDocumentKind.Company, "c1", CompanyPath);
        ledger.RecordSave(UserDocumentKind.Company, "c1", CompanyPath);

        ledger.Sent(Wolves, change: 1, revision: 1);

        var document = ledger.Find(Wolves);
        Assert.Equal(SyncOperation.Save, document?.Pending);
        Assert.Equal(1, document?.Revision);
    }

    [Fact]
    public void Track_UnknownFile_WaitsToBeSent()
    {
        var ledger = new SyncLedger(_records.Paths);

        ledger.Track(UserDocumentKind.Company, "c1", CompanyPath);

        Assert.Equal(SyncOperation.Save, ledger.Find(Wolves)?.Pending);
    }

    [Fact]
    public void Track_KnownFile_LeavesItAlone()
    {
        var ledger = new SyncLedger(_records.Paths);
        ledger.Received(Wolves, CompanyPath, revision: 2);

        ledger.Track(UserDocumentKind.Company, "c1", CompanyPath);

        Assert.Equal(SyncOperation.None, ledger.Find(Wolves)?.Pending);
    }

    [Fact]
    public void RecordSave_NobodySignedInYet_IsUnclaimed()
    {
        var ledger = new SyncLedger(_records.Paths);

        ledger.RecordSave(UserDocumentKind.Company, "c1", CompanyPath);

        Assert.Equal(SyncOwnership.Unclaimed, ledger.Find(Wolves)?.Ownership);
        Assert.Equal(new UnclaimedFiles(1, 0), ledger.Unclaimed());
    }

    [Fact]
    public void RecordSave_SignedIn_IsTheAccounts()
    {
        var ledger = new SyncLedger(_records.Paths);
        ledger.UseAccount("kestrel@example.com");

        ledger.RecordSave(UserDocumentKind.Company, "c1", CompanyPath);

        Assert.Equal(SyncOwnership.Claimed, ledger.Find(Wolves)?.Ownership);
    }

    [Fact]
    public void ClaimAll_Unclaimed_BelongsToTheAccount()
    {
        var ledger = new SyncLedger(_records.Paths);
        ledger.RecordSave(UserDocumentKind.Company, "c1", CompanyPath);

        ledger.ClaimAll();

        Assert.Equal(SyncOwnership.Claimed, ledger.Find(Wolves)?.Ownership);
        Assert.Equal(0, ledger.Unclaimed().Total);
    }

    [Fact]
    public void UseAccount_AnotherAccount_LeavesEverythingUnclaimed()
    {
        var ledger = new SyncLedger(_records.Paths);
        ledger.UseAccount("kestrel@example.com");
        ledger.Received(Wolves, CompanyPath, revision: 5);
        ledger.AddConflict(new SyncConflict { Kind = UserDocumentKind.Company, DocumentId = "c1" });

        ledger.UseAccount("osprey@example.com");

        var document = ledger.Find(Wolves);
        Assert.Equal(0, document?.Revision);
        Assert.Equal(SyncOperation.Save, document?.Pending);
        Assert.Equal(SyncOwnership.Unclaimed, document?.Ownership);
        Assert.Empty(ledger.Conflicts);
    }

    [Fact]
    public void UseAccount_FirstAccount_KeepsWhatWasNoted()
    {
        var ledger = new SyncLedger(_records.Paths);
        ledger.RecordSave(UserDocumentKind.Company, "c1", CompanyPath);

        ledger.UseAccount("kestrel@example.com");

        Assert.Equal(1, ledger.Find(Wolves)?.Change);
    }

    [Fact]
    public async Task WaitForChangeAsync_FileSaved_Returns()
    {
        var ledger = new SyncLedger(_records.Paths);
        var waiting = ledger.WaitForChangeAsync(CancellationToken.None);

        ledger.RecordSave(UserDocumentKind.Company, "c1", CompanyPath);

        await waiting.WaitAsync(TimeSpan.FromSeconds(5));
    }
}
