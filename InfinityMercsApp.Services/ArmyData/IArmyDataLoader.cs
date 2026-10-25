using InfinityMercsApp.Services.CompanySelection.Factions;

namespace InfinityMercsApp.Services.ArmyData;

/// <summary>
/// Has armies in the army data before a screen reads them. The app stores every army when it opens, so it has them
/// already; the web site loads each army the first time a screen needs it.
/// </summary>
public interface IArmyDataLoader
{
    /// <summary>Has these factions' armies (a generated company's faction too) in the army data.</summary>
    Task EnsureAsync(IReadOnlyCollection<int> factionIds, CancellationToken cancellationToken = default);

    /// <summary>Has every army in the army data, for a screen that compares them all.</summary>
    Task EnsureEveryArmyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Which factions the company types that judge every army list, worked out ahead (by the API), so listing them
    /// needn't read every army; null when there are none, and the screens work them out themselves.
    /// </summary>
    FactionListings? Listings { get; }
}
