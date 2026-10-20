namespace InfinityMercsApp.Infrastructure.API.ArmyMirror;

/// <summary>The app's own copy of the army data, which says which army versions it holds.</summary>
public interface IArmyMirror
{
    /// <summary>The copy's manifest, or null when the copy can't be reached; callers then download everything.</summary>
    Task<ArmyMirrorManifest?> GetManifestAsync(CancellationToken cancellationToken = default);
}
