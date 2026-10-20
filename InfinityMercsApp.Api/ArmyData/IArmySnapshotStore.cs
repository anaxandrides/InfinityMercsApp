using InfinityMercsApp.Infrastructure.API.ArmyMirror;

namespace InfinityMercsApp.Api.ArmyData;

/// <summary>The mirror's snapshots: every distinct set of army data it has held, and which one is current.</summary>
public interface IArmySnapshotStore
{
    /// <summary>The current snapshot, or null before the first copy.</summary>
    Task<ArmyMirrorManifest?> ReadLatestAsync(CancellationToken cancellationToken = default);

    /// <summary>The snapshot <paramref name="snapshotId"/>, or null when there is no such snapshot.</summary>
    Task<ArmyMirrorManifest?> ReadAsync(string snapshotId, CancellationToken cancellationToken = default);

    /// <summary>Keeps <paramref name="snapshot"/> for good under its id.</summary>
    Task SaveAsync(ArmyMirrorManifest snapshot, CancellationToken cancellationToken = default);

    /// <summary>Makes <paramref name="manifest"/> the current snapshot.</summary>
    Task SetLatestAsync(ArmyMirrorManifest manifest, CancellationToken cancellationToken = default);
}
