using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season.Marketplace;

namespace InfinityMercsApp.Domain.Season.Inventory;

/// <summary>
/// The gear a company owns at this point in the season: everything bought from the markets (as the catalog
/// knows it) or won in downtime, less what has been sold. Exchange items are spent on purchase and not kept.
/// </summary>
public sealed class SeasonInventory
{
    /// <summary>The downtime result that wins a pistol without listing it as a won item.</summary>
    public const string RandomPistol = "Random pistol";

    private SeasonInventory(IReadOnlyList<InventoryEntry> entries) => Entries = entries;

    public static SeasonInventory Empty { get; } = new([]);

    /// <summary>What is owned, by category (gear slots first, in slot order) and then name.</summary>
    public IReadOnlyList<InventoryEntry> Entries { get; }

    /// <summary>The names of the owned items that go in <paramref name="slot"/>; the extra slot takes any but armour.</summary>
    public IReadOnlyList<string> ItemsFor(GearSlot slot) =>
        Entries.Where(entry => GearSlots.TryParse(entry.Category, out var kind) &&
                               (kind == slot || (slot == GearSlot.Extra && GearSlots.ExtraSlotTakes.Contains(kind))))
            .Select(entry => entry.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static SeasonInventory From(SeasonFile? seasonFile, InventoryCatalog catalog)
    {
        if (seasonFile is null)
        {
            return Empty;
        }

        var tally = new Tally();
        foreach (var transaction in seasonFile.InitialPurchases.Transactions)
        {
            tally.Record(transaction, catalog, roundIndex: 0);
        }

        foreach (var round in seasonFile.Rounds.OrderBy(round => round.RoundIndex))
        {
            foreach (var transaction in round.Marketplace.Transactions)
            {
                tally.Record(transaction, catalog, round.RoundIndex);
            }

            foreach (var won in round.Downtime.WonItems)
            {
                var known = catalog.Find(won.Name);
                var category = string.IsNullOrWhiteSpace(won.Category) ? known?.Category : won.Category;
                var source = string.IsNullOrWhiteSpace(won.Source) ? "Downtime" : won.Source;
                tally.Add(won.Name, category, source, known?.CostCr ?? 0, round.RoundIndex);
            }

            if (round.Downtime.WonItems.Count == 0 &&
                round.Downtime.OtherEffects.Contains(RandomPistol, StringComparison.OrdinalIgnoreCase))
            {
                tally.Add(RandomPistol, GearSlot.Sidearm.Key(), "Downtime", 0, round.RoundIndex);
            }
        }

        return new SeasonInventory(tally.Entries());
    }

    private sealed class Tally
    {
        private readonly Dictionary<string, Bucket> _buckets = new(StringComparer.OrdinalIgnoreCase);

        public void Record(SeasonTransaction transaction, InventoryCatalog catalog, int roundIndex)
        {
            if (transaction.IsSale)
            {
                Remove(transaction.ItemName);
                return;
            }

            var storeName = StoreLabels.StoreName(transaction.OriginStore);
            if (ServiceStoreNames.Contains(storeName) || catalog.Find(storeName, transaction.ItemName) is not { } item)
            {
                return;
            }

            Add(item.Name, item.Category, item.StoreName, item.CostCr, roundIndex);
        }

        public void Add(string name, string? category, string source, int costCr, int roundIndex)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            var normalizedCategory = InventoryCategories.Normalize(category);
            if (string.Equals(normalizedCategory, InventoryCategories.Exchange, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var key = $"{normalizedCategory}|{name.Trim()}";
            if (!_buckets.TryGetValue(key, out var bucket))
            {
                bucket = new Bucket(name.Trim(), normalizedCategory);
                _buckets[key] = bucket;
            }

            if (bucket.CostCr == 0)
            {
                bucket.CostCr = costCr;
            }

            bucket.Count++;
            if (!string.IsNullOrWhiteSpace(source))
            {
                bucket.Sources.Add(source.Trim());
            }

            bucket.Rounds.Add(roundIndex);
        }

        // A sale takes one from the first holding of that name, whatever its category.
        private void Remove(string name)
        {
            var bucket = _buckets.Values.FirstOrDefault(candidate =>
                candidate.Count > 0 && string.Equals(candidate.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (bucket is not null)
            {
                bucket.Count--;
            }
        }

        public IReadOnlyList<InventoryEntry> Entries() =>
            _buckets.Values
                .Where(bucket => bucket.Count > 0)
                .OrderBy(bucket => InventoryCategories.Order(bucket.Category))
                .ThenBy(bucket => bucket.Category, StringComparer.Ordinal)
                .ThenBy(bucket => bucket.Name, StringComparer.Ordinal)
                .Select(bucket => new InventoryEntry(bucket.Name, bucket.Category, bucket.CostCr, bucket.Count, bucket.Sources.ToList(), bucket.Rounds.ToList()))
                .ToList();
    }

    private sealed class Bucket(string name, string category)
    {
        public string Name { get; } = name;
        public string Category { get; } = category;
        public int CostCr { get; set; }
        public int Count { get; set; }
        public SortedSet<string> Sources { get; } = new(StringComparer.OrdinalIgnoreCase);
        public SortedSet<int> Rounds { get; } = [];
    }
}
