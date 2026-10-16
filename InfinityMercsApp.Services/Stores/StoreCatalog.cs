using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season.Inventory;

namespace InfinityMercsApp.Services.Stores;

/// <summary>
/// Every store, read once, and the catalog of their stock, which gives owned gear its category, price and SWC.
/// </summary>
public sealed class StoreCatalog(IStoreProvider storeProvider)
{
    private IReadOnlyList<Store>? _stores;
    private InventoryCatalog _catalog = InventoryCatalog.Empty;

    /// <summary>Every store that could be read.</summary>
    public async Task<IReadOnlyList<Store>> AllAsync()
    {
        if (_stores is null)
        {
            List<Store> stores = [];
            foreach (var name in storeProvider.GetAllStoreNames())
            {
                if (await storeProvider.GetStoreByNameAsync(name) is { } store)
                {
                    stores.Add(store);
                }
            }

            _stores = stores;
            _catalog = InventoryCatalog.From(stores);
        }

        return _stores;
    }

    /// <summary>What every store sells.</summary>
    public async Task<InventoryCatalog> CatalogAsync()
    {
        await AllAsync();
        return _catalog;
    }
}
