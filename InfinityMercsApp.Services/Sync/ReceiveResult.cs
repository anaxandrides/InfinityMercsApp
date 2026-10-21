namespace InfinityMercsApp.Services.Sync;

/// <summary>How fetching one kind of file went, and how many files it fetched or deleted.</summary>
internal sealed record ReceiveResult(SyncStep Step, int Files);
