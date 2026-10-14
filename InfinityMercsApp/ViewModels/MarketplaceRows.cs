using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season;

namespace InfinityMercsApp.ViewModels;

/// <summary>Something a marketplace sells, with the quantity added to the cart.</summary>
public abstract class MarketplaceRow(string cartName, int costCr) : ObservableObject
{
    private int _quantity;

    /// <summary>The name shown on the row's cart line.</summary>
    public string CartName { get; } = cartName;

    public int CostCr { get; } = costCr;

    public virtual string CostDisplay => $"{CostCr}cr";

    /// <summary>How many are in the cart; never below zero.</summary>
    public int Quantity
    {
        get => _quantity;
        set => SetProperty(ref _quantity, Math.Max(0, value));
    }
}

public sealed class StoreItemRow(StoreItem item) : MarketplaceRow(item.Name, item.CostCr)
{
    public string Name => item.Name;
    public decimal? CostSwc => item.CostSwc;
    public string Category => item.Category;
    public string? WikiUrl => item.WikiUrl;
    public string? WikiSection => item.WikiSection;

    public override string CostDisplay => SeasonEconomy.PriceText(CostCr, CostSwc);
}

public sealed class TroopTypeRow(StoreTroopType troopType) : MarketplaceRow(DisplayNameFor(troopType), troopType.CostCr)
{
    public string DisplayName => CartName;
    public string ArmDisplay => troopType.Arm?.ToString() ?? "—";
    public string BtsDisplay => troopType.Bts?.ToString() ?? "—";
    public string Abilities => troopType.Abilities ?? string.Empty;
    public bool HasAbilities => !string.IsNullOrWhiteSpace(Abilities);
    public string StatDisplay => $"{Stat.Arm.Key()} {ArmDisplay}  |  {Stat.Bts.Key()} {BtsDisplay}";

    // "Light Armour (LI)", or just the armour when the troop type is not named.
    private static string DisplayNameFor(StoreTroopType troopType) =>
        string.IsNullOrWhiteSpace(troopType.Type) ? troopType.ArmorName : $"{troopType.ArmorName} ({troopType.Type})";
}

public sealed class AugmentRow(StoreAugment augment) : MarketplaceRow(augment.Name, augment.CostCr)
{
    public string Name => augment.Name;
    public string? Requirement => augment.Requirement;
    public bool HasRequirement => !string.IsNullOrWhiteSpace(Requirement);
    public string RequirementDisplay => Requirement ?? "—";
    public string CostNote => augment.CostNote ?? string.Empty;
}

/// <summary>A store's items in one category, in the order the store lists them.</summary>
public sealed record StoreItemGroup(string Category, IReadOnlyList<StoreItemRow> Items)
{
    /// <summary>The items grouped by category, keeping the store's order of first appearance.</summary>
    public static IReadOnlyList<StoreItemGroup> From(IEnumerable<StoreItem> items) =>
        items.GroupBy(item => item.Category)
            .Select(group => new StoreItemGroup(group.Key, group.Select(item => new StoreItemRow(item)).ToList()))
            .ToList();
}

/// <summary>A cart line: how many of a row are being bought and what they cost together.</summary>
public sealed record MarketplaceCartLine(string Name, int Quantity, int LineCost)
{
    public string QuantityDisplay => $"x{Quantity}";
    public string CostDisplay => $"{LineCost}cr";

    /// <summary>A line for each row with a quantity, in the order given.</summary>
    public static IReadOnlyList<MarketplaceCartLine> From(IEnumerable<MarketplaceRow> rows) =>
        rows.Where(row => row.Quantity > 0)
            .Select(row => new MarketplaceCartLine(row.CartName, row.Quantity, row.CostCr * row.Quantity))
            .ToList();
}

/// <summary>A labelled value in the detail popup, e.g. Cost 12cr.</summary>
public sealed record DetailRow(string Label, string Value);

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

    public static MarketplaceDetail For(AugmentRow augment)
    {
        List<DetailRow> rows = [new("Cost", augment.CostDisplay)];
        if (augment.HasRequirement)
        {
            rows.Add(new("Requires", augment.Requirement!));
        }

        if (!string.IsNullOrWhiteSpace(augment.CostNote))
        {
            rows.Add(new("Note", augment.CostNote));
        }

        return new(augment.Name, rows);
    }
}

/// <summary>Where the detail popup's wiki description has got to.</summary>
public enum WikiStatus
{
    /// <summary>The row has no wiki page.</summary>
    None,
    Loading,

    /// <summary>The page was fetched but had nothing to show, or could not be fetched.</summary>
    Empty,
    Loaded
}
