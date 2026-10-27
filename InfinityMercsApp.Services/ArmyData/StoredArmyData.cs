using InfinityMercsApp.Services.CompanySelection.Factions;

namespace InfinityMercsApp.Services.ArmyData;

/// <inheritdoc/>
/// <remarks>
/// The app's: it stores every army when it opens (the splash screen), so there is nothing left to load, and the screens
/// work out their faction lists from the stored armies.
/// </remarks>
public sealed class StoredArmyData : IArmyDataLoader
{
    /// <inheritdoc/>
    public FactionListings? Listings => null;

    /// <inheritdoc/>
    public Task EnsureAsync(IReadOnlyCollection<int> factionIds, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task EnsureEveryArmyAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
