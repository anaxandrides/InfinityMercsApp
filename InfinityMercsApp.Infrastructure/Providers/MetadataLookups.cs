using InfinityMercsApp.Domain.Models.Metadata;

namespace InfinityMercsApp.Infrastructure.Providers;

public static class MetadataLookups
{
    /// <summary>
    /// The weapon profiles for an item such as "Combi Rifle (+1B)", searched by its name before the bracket:
    /// the profiles with exactly that name, or every search match when none is exact.
    /// </summary>
    public static IReadOnlyList<Weapon> WeaponsFor(this IMetadataProvider metadata, string itemName)
    {
        var name = WikiLinks.BaseName(itemName);
        var matches = metadata.SearchWeaponsByName(name);
        var exact = matches.Where(weapon => string.Equals(weapon.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
        return exact.Count > 0 ? exact : matches.ToList();
    }
}
