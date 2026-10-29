using System.Text.Json;
using System.Text.Json.Serialization;
using InfinityMercsApp.ApiClient.UserDocuments;
using InfinityMercsApp.Services.Sync;
using InfinityMercsApp.UnitTests.Support;

namespace InfinityMercsApp.UnitTests.Services.Sync;

/// <summary>The ledger's file reads and writes as it did when it went through reflection, with every enum as text.</summary>
public sealed class SyncLedgerFileTests : IDisposable
{
    // How the ledger was written before its JSON was source-generated.
    private static readonly JsonSerializerOptions Before = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };

    private readonly TempRecords _records = new();

    public void Dispose() => _records.Dispose();

    [Fact]
    public void Load_WrittenTheOldWay_ReadsEverything()
    {
        var state = FilledObjects.Of<SyncState>() with { Account = "kestrel@example.com" };
        Directory.CreateDirectory(Path.GetDirectoryName(_records.Paths.SyncStatePath) ?? ".");
        File.WriteAllText(_records.Paths.SyncStatePath, JsonSerializer.Serialize(state, Before));

        var ledger = new SyncLedger(_records.Paths);

        Assert.Equal("kestrel@example.com", ledger.Account);
        Assert.Equal(state.Documents, ledger.Documents);
        Assert.Equal(state.Conflicts, ledger.Conflicts);
    }

    [Fact]
    public void Save_Ledger_WritesWhatTheOldWayWrote()
    {
        var ledger = new SyncLedger(_records.Paths);
        ledger.UseAccount("kestrel@example.com");
        ledger.RecordSave(UserDocumentKind.Company, "c1", Path.Combine(_records.Paths.CompaniesDirectory, "wolves-0001.json"));
        ledger.AddConflict(new SyncConflict { Kind = UserDocumentKind.Season, DocumentId = "s1", OtherDeleted = true });

        var written = File.ReadAllText(_records.Paths.SyncStatePath);

        Assert.Contains("\"Save\"", written, StringComparison.Ordinal);
        Assert.Contains("\"Season\"", written, StringComparison.Ordinal);
        Assert.Equal(written, JsonSerializer.Serialize(JsonSerializer.Deserialize<SyncState>(written, Before), Before));
    }
}
