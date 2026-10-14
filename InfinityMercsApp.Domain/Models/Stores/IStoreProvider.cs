namespace InfinityMercsApp.Domain.Models.Stores;

public interface IStoreProvider
{
    /// <summary>
    /// The stores available to the given factions. Stores tied to no faction are always included; otherwise a
    /// store's faction list is matched against the supplied names (case-insensitive).
    /// </summary>
    /// <param name="factionNames">Faction names to match against store availability.</param>
    Task<IReadOnlyList<Store>> GetAvailableStoresAsync(
        IReadOnlyList<string> factionNames,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the names of all known stores in their canonical display form.
    /// </summary>
    IReadOnlyList<string> GetAllStoreNames();

    /// <summary>
    /// Loads and returns the full store by name (case-insensitive), or null if not found.
    /// </summary>
    Task<Store?> GetStoreByNameAsync(string name, CancellationToken cancellationToken = default);
}
