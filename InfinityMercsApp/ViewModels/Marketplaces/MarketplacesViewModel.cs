using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.ViewModels.Marketplaces.Items;
using InfinityMercsApp.ViewModels.Components;

using InfinityMercsApp.Presentation.Marketplaces;
namespace InfinityMercsApp.ViewModels.Marketplaces;

/// <summary>
/// Browses one marketplace at a time: its items by category, troop types and augments, a detail popup for
/// each, and (on wide screens) a cart that totals the quantities picked.
/// </summary>
public partial class MarketplacesViewModel : AdaptiveViewModelBase<MarketplacesLayout>
{
    private readonly IStoreProvider _storeProvider;

    public MarketplacesViewModel(
        INavigationService navigationService,
        IStoreProvider storeProvider,
        MarketplaceDetailViewModel detailPopup) : base(navigationService, MarketplacesLayout.For)
    {
        _storeProvider = storeProvider;
        DetailPopup = detailPopup;
        StoreNames = storeProvider.GetAllStoreNames();
    }

    public IReadOnlyList<string> StoreNames { get; }

    /// <summary>The popup showing a row in full.</summary>
    public MarketplaceDetailViewModel DetailPopup { get; }

    [ObservableProperty]
    public partial string? SelectedStoreName { get; set; }

    [ObservableProperty]
    public partial bool HasStore { get; set; }

    /// <summary>The cart shows once a store is open, on layouts that offer buying.</summary>
    [ObservableProperty]
    public partial bool ShowCart { get; set; }

    [ObservableProperty]
    public partial string StoreName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StoreFactionsDisplay { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StoreMetaDisplay { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasItems))]
    public partial IReadOnlyList<StoreItemGroup> ItemGroups { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTroopTypes))]
    public partial IReadOnlyList<TroopTypeRow> TroopTypeRows { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAugments))]
    public partial IReadOnlyList<AugmentRow> AugmentRows { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCartLines), nameof(IsCartEmpty))]
    public partial IReadOnlyList<MarketplaceCartLine> CartLines { get; set; } = [];

    [ObservableProperty]
    public partial string CartTotalDisplay { get; set; } = "Total: 0cr";

    public bool HasItems => ItemGroups.Count > 0;
    public bool HasTroopTypes => TroopTypeRows.Count > 0;
    public bool HasAugments => AugmentRows.Count > 0;
    public bool HasCartLines => CartLines.Count > 0;
    public bool IsCartEmpty => CartLines.Count == 0;

    protected override void OnLayoutApplied() => ShowCart = HasStore && Layout.OffersBuying;

    partial void OnHasStoreChanged(bool value) => ShowCart = value && Layout.OffersBuying;

    partial void OnSelectedStoreNameChanged(string? value) => SelectStoreCommand.Execute(value);

    /// <summary>Shows the named store with an empty cart; no name hides the store, and an unknown one changes nothing.</summary>
    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task SelectStoreAsync(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            HasStore = false;
            return;
        }

        var store = await _storeProvider.GetStoreByNameAsync(name);

        // A later pick replaces this one, even if this store finished loading last.
        if (store is null || (SelectedStoreName is not null && SelectedStoreName != name))
        {
            return;
        }

        StoreName = store.Name;
        StoreFactionsDisplay = store.AssociatedFactions.Count > 0
            ? $"Factions: {string.Join(", ", store.AssociatedFactions)}"
            : "Factions: All (Neutral)";
        StoreMetaDisplay = MetaText(store);
        ItemGroups = StoreItemGroup.From(store.Items);
        TroopTypeRows = store.TroopTypes.Select(troopType => new TroopTypeRow(troopType)).ToList();
        AugmentRows = store.Augments.Select(augment => new AugmentRow(augment)).ToList();
        RefreshCart();
        HasStore = true;
    }

    [RelayCommand]
    private void Increment(MarketplaceRow row)
    {
        row.Quantity++;
        RefreshCart();
    }

    [RelayCommand]
    private void Decrement(MarketplaceRow row)
    {
        row.Quantity--;
        RefreshCart();
    }

    /// <summary>Opens the detail popup for a row; an item with a wiki page then loads its description.</summary>
    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task ShowDetailAsync(MarketplaceRow row) => DetailPopup.ShowRowAsync(row);

    private void RefreshCart()
    {
        IEnumerable<MarketplaceRow> rows =
        [
            .. ItemGroups.SelectMany(group => group.Items),
            .. TroopTypeRows,
            .. AugmentRows
        ];
        CartLines = MarketplaceCartLine.From(rows);
        CartTotalDisplay = $"Total: {CartLines.Sum(line => line.LineCost)}cr";
    }

    // "Type: Mercenary   |   Alignment: Chaotic", leaving out whichever the store does not have.
    private static string MetaText(Store store)
    {
        List<string> parts = [];
        if (!string.IsNullOrWhiteSpace(store.AssociatedType))
        {
            parts.Add($"Type: {store.AssociatedType}");
        }

        if (!string.IsNullOrWhiteSpace(store.Alignment))
        {
            parts.Add($"Alignment: {store.Alignment}");
        }

        return string.Join("   |   ", parts);
    }
}
