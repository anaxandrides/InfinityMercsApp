using System.Globalization;
using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Services.ArmyData;

namespace InfinityMercsApp.Services.CompanySelection.StartCompany;

/// <summary>What a trooper of a faction can buy with one-off credits: the Base Market's and its faction's War Market's gear.</summary>
public sealed class StartingGearCatalog(IStoreProvider stores, IArmyDataService armyData)
{
    /// <summary>
    /// Each item <see cref="StartingGear"/> offers from the markets open to <paramref name="factionId"/> and its vanilla
    /// parent, cheapest first; an item two markets sell is listed once, at the lower price.
    /// </summary>
    public async Task<IReadOnlyList<StartingGearOption>> ForAsync(int factionId, CancellationToken cancellationToken = default)
    {
        var factionNames = FactionNames(factionId);
        var weaponNames = armyData.GetWeaponNames().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var markets = (await stores.GetAvailableStoresAsync(factionNames.ToList(), cancellationToken))
            .Where(store => !store.IsServiceStore && (!store.IsWarMarket || store.AssociatedFactions.Any(factionNames.Contains)));

        var offers = new Dictionary<string, StartingGearOption>(StringComparer.OrdinalIgnoreCase);
        foreach (var store in markets)
        {
            foreach (var item in store.Items.Where(StartingGear.IsOffered))
            {
                var key = item.Name.Trim();
                if (!offers.TryGetValue(key, out var listed) || item.CostCr < listed.Cost)
                {
                    offers[key] = Option(store.Name, item, weaponNames);
                }
            }
        }

        return offers.Values
            .OrderBy(option => option.Cost)
            .ThenBy(option => option.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static StartingGearOption Option(string storeName, StoreItem item, IReadOnlySet<string> weaponNames) =>
        new($"{item.Name.Trim()} ({item.CostCr.ToString(CultureInfo.InvariantCulture)} CR, {storeName})", item.CostCr, StartingGear.UpgradeFor(item, weaponNames));

    // The faction's name and its vanilla parent's, as the markets list the factions they serve.
    private IReadOnlySet<string> FactionNames(int factionId) =>
        armyData.GetMetadataFactionById(factionId) is { } faction
            ? FactionResolver.GetExpandedFactionNames([faction], armyData.GetMetadataFactions())
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
