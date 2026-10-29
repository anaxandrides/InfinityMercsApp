using System.Text.Json.Serialization;

namespace InfinityMercsApp.Services.Sync;

/// <summary>Source-generated JSON contract for the sync ledger's state file.</summary>
[JsonSerializable(typeof(SyncState))]
internal sealed partial class SyncJsonContext : JsonSerializerContext;
