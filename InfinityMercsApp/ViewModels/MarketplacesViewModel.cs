using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels;

/// <summary>
/// Browses one marketplace at a time: its items by category, troop types and augments, a detail popup for
/// each, and (on wide screens) a cart that totals the quantities picked.
/// </summary>
public partial class MarketplacesViewModel : ViewModelBase
{
    private readonly IStoreProvider _storeProvider;
    private readonly IMetadataProvider _metadataProvider;
    private readonly IWikiDescriptionService _wikiDescriptionService;
    private readonly ILauncher _launcher;
    private SkillWikiLinks? _skillLinks;

    public MarketplacesViewModel(
        INavigationService navigationService,
        IStoreProvider storeProvider,
        IMetadataProvider metadataProvider,
        IWikiDescriptionService wikiDescriptionService,
        ILauncher launcher) : base(navigationService)
    {
        _storeProvider = storeProvider;
        _metadataProvider = metadataProvider;
        _wikiDescriptionService = wikiDescriptionService;
        _launcher = launcher;
        StoreNames = storeProvider.GetAllStoreNames();
    }

    public IReadOnlyList<string> StoreNames { get; }

    [ObservableProperty]
    private string? _selectedStoreName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCart))]
    private bool _hasStore;

    /// <summary>Buying is offered only on the widest screens; the page sets this as its layout changes.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCart))]
    private bool _showQuantityControls;

    [ObservableProperty]
    private string _storeName = string.Empty;

    [ObservableProperty]
    private string _storeFactionsDisplay = string.Empty;

    [ObservableProperty]
    private string _storeMetaDisplay = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasItems))]
    private IReadOnlyList<StoreItemGroup> _itemGroups = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTroopTypes))]
    private IReadOnlyList<TroopTypeRow> _troopTypeRows = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAugments))]
    private IReadOnlyList<AugmentRow> _augmentRows = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCartLines), nameof(IsCartEmpty))]
    private IReadOnlyList<MarketplaceCartLine> _cartLines = [];

    [ObservableProperty]
    private string _cartTotalDisplay = "Total: 0cr";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDetailOpen))]
    private MarketplaceDetail? _detail;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasWiki), nameof(IsWikiLoading), nameof(IsWikiEmpty), nameof(HasWikiBlocks))]
    private WikiStatus _wikiStatus;

    [ObservableProperty]
    private IReadOnlyList<WikiContentBlock> _wikiBlocks = [];

    public bool ShowCart => HasStore && ShowQuantityControls;
    public bool HasItems => ItemGroups.Count > 0;
    public bool HasTroopTypes => TroopTypeRows.Count > 0;
    public bool HasAugments => AugmentRows.Count > 0;
    public bool HasCartLines => CartLines.Count > 0;
    public bool IsCartEmpty => CartLines.Count == 0;
    public bool IsDetailOpen => Detail is not null;
    public bool HasWiki => WikiStatus != WikiStatus.None;
    public bool IsWikiLoading => WikiStatus == WikiStatus.Loading;
    public bool IsWikiEmpty => WikiStatus == WikiStatus.Empty;
    public bool HasWikiBlocks => WikiStatus == WikiStatus.Loaded;

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
    private async Task ShowDetailAsync(MarketplaceRow row)
    {
        var detail = row switch
        {
            StoreItemRow item => MarketplaceDetail.For(item, _metadataProvider.WeaponsFor(item.Name)),
            TroopTypeRow troopType => MarketplaceDetail.For(troopType, _skillLinks ??= SkillWikiLinks.From(_metadataProvider.GetSkills())),
            AugmentRow augment => MarketplaceDetail.For(augment),
            _ => null
        };
        if (detail is null)
        {
            return;
        }

        WikiBlocks = [];
        WikiStatus = detail.WikiUrl is null ? WikiStatus.None : WikiStatus.Loading;
        Detail = detail;
        if (detail.WikiUrl is null)
        {
            return;
        }

        var blocks = await _wikiDescriptionService.FetchContentAsync(detail.WikiUrl, detail.WikiSection);

        // The popup may have been closed or moved on to another row while the page loaded.
        if (!ReferenceEquals(Detail, detail))
        {
            return;
        }

        WikiBlocks = blocks;
        WikiStatus = blocks.Count == 0 ? WikiStatus.Empty : WikiStatus.Loaded;
    }

    [RelayCommand]
    private void CloseDetail() => Detail = null;

    [RelayCommand]
    private async Task OpenLinkAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            await _launcher.OpenAsync(url);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to open link '{url}': {ex.Message}");
        }
    }

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
