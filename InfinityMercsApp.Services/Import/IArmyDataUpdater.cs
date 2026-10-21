namespace InfinityMercsApp.Services.Import;

/// <summary>
/// Keeps the stored army data in step with the Infinity Army API, the data's source of truth, read through the app's
/// own copy of it. Checking downloads what may have changed and compares it with what is stored; applying stores a
/// check's changes.
/// </summary>
public interface IArmyDataUpdater
{
    /// <summary>True when metadata and armies from an earlier download are stored, so the app can run on them.</summary>
    bool HasStoredData();

    /// <summary>
    /// Downloads the metadata and every army not already stored at the version the copy holds, and compares them with
    /// what is stored. Never throws for a failed download.
    /// </summary>
    Task<ArmyDataCheck> CheckAsync(IProgress<ArmyDataProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>Stores a successful check's changes and rebuilds the generated company factions.</summary>
    /// <returns>The factions whose army could not be stored.</returns>
    Task<IReadOnlyList<string>> ApplyAsync(ArmyDataCheck check, IProgress<ArmyDataProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds the generated company factions from the stored armies when they were built with older rules than
    /// the app's, so a fix to who may join them reaches the app without waiting for new army data.
    /// </summary>
    /// <returns>True when they were rebuilt.</returns>
    bool RebuildOutdatedGeneratedCompanies(IProgress<ArmyDataProgress>? progress = null, CancellationToken cancellationToken = default);
}
