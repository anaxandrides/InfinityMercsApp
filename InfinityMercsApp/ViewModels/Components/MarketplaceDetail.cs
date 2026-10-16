using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Domain.Models.Metadata.Wiki;
using InfinityMercsApp.ViewModels.Marketplaces.Items;

namespace InfinityMercsApp.ViewModels.Components;

/// <summary>
/// What the detail popup shows for a row: its facts, then (for troop types) its abilities, and (for items)
/// the matching weapon profiles. An item with a wiki link also has its wiki description loaded.
/// </summary>
public sealed record MarketplaceDetail(string Title, IReadOnlyList<DetailRow> Rows)
{
    public IReadOnlyList<AbilityLink> Abilities { get; init; } = [];
    public IReadOnlyList<Weapon> Weapons { get; init; } = [];
    public string? WikiUrl { get; init; }
    public string? WikiSection { get; init; }

    public bool HasAbilities => Abilities.Count > 0;

    public static MarketplaceDetail For(StoreItemRow item, IReadOnlyList<Weapon> weapons) =>
        new(item.Name, [new("Cost", item.CostDisplay), new("Category", item.Category)])
        {
            Weapons = weapons,
            WikiUrl = string.IsNullOrWhiteSpace(item.WikiUrl) ? null : item.WikiUrl,
            WikiSection = item.WikiSection
        };

    public static MarketplaceDetail For(TroopTypeRow troopType, SkillWikiLinks skillLinks) =>
        new(troopType.DisplayName,
            [
                new("Cost", troopType.CostDisplay),
                new(Stat.Arm.Key(), troopType.ArmDisplay),
                new(Stat.Bts.Key(), troopType.BtsDisplay)
            ])
        {
            Abilities = skillLinks.Links(troopType.Abilities)
        };

    /// <summary>A weapon's profiles, titled with its name.</summary>
    public static MarketplaceDetail ForWeapon(string weaponName, IReadOnlyList<Weapon> weapons) =>
        new(weaponName, []) { Weapons = weapons };

    public static MarketplaceDetail For(AugmentRow augment)
    {
        List<DetailRow> rows = [new("Cost", augment.CostDisplay)];
        if (augment.HasRequirement && augment.Requirement is { } requirement)
        {
            rows.Add(new("Requires", requirement));
        }

        if (!string.IsNullOrWhiteSpace(augment.CostNote))
        {
            rows.Add(new("Note", augment.CostNote));
        }

        return new(augment.Name, rows);
    }
}
