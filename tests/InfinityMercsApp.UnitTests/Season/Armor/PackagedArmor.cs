using System.Text.Json;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.UnitTests.Characterization;

namespace InfinityMercsApp.UnitTests.Season.Armor;

/// <summary>The armour tables of the stores the app ships, read as the store provider reads them.</summary>
internal static class PackagedArmor
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly Lazy<IReadOnlyList<Store>> Stores = new(() =>
        Directory.GetFiles(Path.Combine(RepoFiles.RepoRoot(), "InfinityMercsApp", "Resources", "Raw", "Stores"), "store-*.json")
            .Select(path => JsonSerializer.Deserialize<Store>(File.ReadAllText(path), JsonOptions) ?? new Store())
            .ToList());

    public static IReadOnlyList<StoreTroopType> All => [.. Stores.Value.SelectMany(store => store.TroopTypes)];

    public static StoreTroopType Named(string armorName) => All.Single(armor => armor.ArmorName == armorName);
}
