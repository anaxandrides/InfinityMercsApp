using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season.Marketplace;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Marketplaces;

namespace InfinityMercsApp.ViewModels.Season.Overview;

/// <summary>
/// The season's market tab: the stores open to the company (its markets, the round's temporary store, then
/// the services), one store's stock at notoriety-adjusted prices, and a cart kept across stores. What is
/// left to spend is the season's balance less the cart; buying records the cart in the latest round and
/// then calls <c>purchased</c>.
/// </summary>
public partial class SeasonMarketplaceViewModel(
    IStoreProvider storeProvider,
    ISeasonFileService seasonFiles,
    IDialogService dialogs,
    MarketplaceDetailViewModel detailPopup,
    Func<Task> purchased) : ObservableObject
{
    private readonly Dictionary<string, SeasonCartLine> _cart = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<Store> _availableStores = [];
    private string? _temporaryStoreName;
    private string? _seasonFilePath;
    private string _shownStoreName = string.Empty;
    private SeasonLedger _ledger;
    private int _companyNotoriety;
    private bool _choosingStore;

    [ObservableProperty]
    private IReadOnlyList<StoreChoice> _stores = [];

    [ObservableProperty]
    private StoreChoice? _selectedStore;

    [ObservableProperty]
    private bool _hasStore;

    [ObservableProperty]
    private string _storeName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAlignmentText))]
    private string _alignmentText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<StoreItemGroup> _itemGroups = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTroopTypes))]
    private IReadOnlyList<TroopTypeRow> _troopTypeRows = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAugments))]
    private IReadOnlyList<AugmentRow> _augmentRows = [];

    /// <summary>What is left to spend once the cart is paid for, e.g. "12 CR - 1.5 SWC".</summary>
    [ObservableProperty]
    private string _resourcesText = SeasonEconomy.BalanceText(0, 0);

    /// <summary>The popup showing a row in full.</summary>
    public MarketplaceDetailViewModel DetailPopup => detailPopup;

    public bool HasAlignmentText => AlignmentText.Length > 0;
    public bool HasTroopTypes => TroopTypeRows.Count > 0;
    public bool HasAugments => AugmentRows.Count > 0;
    public bool HasCartItems => _cart.Count > 0;

    /// <summary>Takes in the season's balance and the company's notoriety.</summary>
    public void RefreshBalance(SeasonFile? seasonFile)
    {
        _ledger = SeasonEconomy.Ledger(seasonFile);
        _companyNotoriety = SeasonRounds.CompanyNotoriety(seasonFile);
        RefreshAffordability();
    }

    /// <summary>Lists the stores open to factions with these names, and shows the first.</summary>
    public async Task LoadStoresAsync(IReadOnlySet<string> factionNames, string? seasonFilePath, SeasonFile? seasonFile)
    {
        _seasonFilePath = seasonFilePath;
        _availableStores = await storeProvider.GetAvailableStoresAsync(factionNames.ToList());

        List<StoreChoice> choices = [.. _availableStores.Where(store => !store.IsServiceStore).Select(store => new StoreChoice(StoreLabels.For(store), store.Name))];

        // The latest round's temporary store, unless the company has that store anyway.
        var temporaryRound = seasonFile?.Rounds
            .OrderByDescending(round => round.RoundIndex)
            .FirstOrDefault(round => !string.IsNullOrWhiteSpace(round.TemporaryStore));
        _temporaryStoreName = temporaryRound is not null &&
                              !_availableStores.Any(store => string.Equals(store.Name, temporaryRound.TemporaryStore, StringComparison.OrdinalIgnoreCase))
            ? temporaryRound.TemporaryStore
            : null;
        if (temporaryRound is not null && _temporaryStoreName is not null)
        {
            choices.Add(new StoreChoice(StoreLabels.ForTemporary(_temporaryStoreName, temporaryRound.RoundIndex), _temporaryStoreName));
        }

        // Medical Services, then Additional Recruitment, always last.
        choices.AddRange(_availableStores
            .Where(store => store.IsServiceStore)
            .OrderBy(store => string.Equals(store.Name, ServiceStoreNames.MedicalServices, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .Select(store => new StoreChoice(StoreLabels.For(store), store.Name)));

        Stores = choices;
        _choosingStore = true;
        SelectedStore = choices.FirstOrDefault();
        _choosingStore = false;
        await SelectStoreAsync(SelectedStore);
    }

    /// <summary>The stores the company can shop at this round, by name.</summary>
    public IReadOnlyList<string> StoreNames()
    {
        List<string> names = [.. _availableStores.Select(store => store.Name)];
        if (_temporaryStoreName is not null)
        {
            names.Add(_temporaryStoreName);
        }

        return names;
    }

    partial void OnSelectedStoreChanged(StoreChoice? value)
    {
        if (!_choosingStore)
        {
            SelectStoreCommand.Execute(value);
        }
    }

    /// <summary>Shows a store's stock at the company's prices; the cart keeps what was added elsewhere.</summary>
    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task SelectStoreAsync(StoreChoice? choice)
    {
        DetailPopup.CloseCommand.Execute(null);
        if (choice is null)
        {
            HasStore = false;
            return;
        }

        var store = await storeProvider.GetStoreByNameAsync(choice.StoreName);

        // A later pick replaces this one, even if this store finished loading last.
        if (store is null || SelectedStore != choice)
        {
            return;
        }

        var notoriety = MarketNotoriety.For(store, _companyNotoriety);
        _shownStoreName = store.Name;
        StoreName = store.Name;
        AlignmentText = notoriety.AlignmentText(store.Alignment);
        ItemGroups = store.Items
            .GroupBy(item => InventoryCategories.Normalize(item.Category), StringComparer.OrdinalIgnoreCase)
            .Select(group => new StoreItemGroup(
                group.Key.ToUpperInvariant(),
                group.Select(item => FromCart(new StoreItemRow(item, notoriety.Price(item.CostCr)))).ToList()))
            .ToList();
        TroopTypeRows = store.TroopTypes.Select(troop => FromCart(new TroopTypeRow(troop, notoriety.Price(troop.CostCr)))).ToList();
        AugmentRows = store.Augments.Select(augment => FromCart(new AugmentRow(augment, notoriety.Price(augment.CostCr)))).ToList();
        HasStore = true;
        RefreshAffordability();
    }

    [RelayCommand]
    private void Increment(MarketplaceRow? row)
    {
        if (row is null || !row.IsAffordable)
        {
            return;
        }

        row.Quantity++;
        PutInCart(row);
    }

    [RelayCommand]
    private void Decrement(MarketplaceRow? row)
    {
        if (row is null || row.Quantity == 0)
        {
            return;
        }

        row.Quantity--;
        PutInCart(row);
    }

    [RelayCommand]
    private Task ShowDetailAsync(MarketplaceRow? row) => row is null ? Task.CompletedTask : DetailPopup.ShowRowAsync(row);

    /// <summary>Records the cart in the latest round (exchanges grant their SWC) and empties it.</summary>
    [RelayCommand(CanExecute = nameof(HasCartItems))]
    private async Task BuyAsync()
    {
        var ledger = SeasonEconomy.Ledger(await seasonFiles.LoadAsync(_seasonFilePath));
        var transactions = _cart.Values
            .SelectMany(line => Enumerable.Repeat(line, line.Quantity))
            .Select(line => new SeasonTransaction
            {
                OriginStore = line.StoreName,
                ItemName = line.ItemName,
                CostCr = line.CostCr,
                CostSwc = line.CostSwc > 0 ? line.CostSwc : null,
                SwcGrant = string.Equals(line.Category, InventoryCategories.Exchange, StringComparison.OrdinalIgnoreCase)
                    ? SeasonEconomy.SwcGrantFromItemName(line.ItemName)
                    : null
            })
            .ToList();
        var storeNames = StoreNames().ToList();

        var recorded = await seasonFiles.UpdateLatestRoundAsync(_seasonFilePath, round =>
        {
            if (round.StartingCr == 0 && round.StartingSwc == 0)
            {
                round.StartingCr = ledger.CreditsBalance;
                round.StartingSwc = ledger.SwcBalance;
            }

            round.Marketplace.Transactions.AddRange(transactions);
            round.Marketplace.Stores = storeNames;
        });
        if (!recorded)
        {
            await dialogs.ShowAlertAsync("Nothing Bought", "Purchases are recorded against a round, and this season has not started one yet.");
            return;
        }

        _cart.Clear();
        foreach (var row in Rows())
        {
            row.Quantity = 0;
        }

        RefreshAffordability();
        await purchased();
    }

    private T FromCart<T>(T row) where T : MarketplaceRow
    {
        row.Quantity = _cart.GetValueOrDefault(CartKey(row))?.Quantity ?? 0;
        return row;
    }

    private void PutInCart(MarketplaceRow row)
    {
        if (row.Quantity > 0)
        {
            _cart[CartKey(row)] = new SeasonCartLine(_shownStoreName, row.CartName, row.InventoryCategory, row.CostCr, row.CostSwc, row.Quantity);
        }
        else
        {
            _cart.Remove(CartKey(row));
        }

        RefreshAffordability();
    }

    private string CartKey(MarketplaceRow row) => $"{_shownStoreName}|{row.CartName}";

    private IEnumerable<MarketplaceRow> Rows() =>
        [.. ItemGroups.SelectMany(group => group.Items), .. TroopTypeRows, .. AugmentRows];

    // A row is affordable while one more fits in the balance left after the cart.
    private void RefreshAffordability()
    {
        var remainingCr = _ledger.CreditsBalance - _cart.Values.Sum(line => line.TotalCr);
        var remainingSwc = _ledger.SwcBalance - _cart.Values.Sum(line => line.TotalSwc);
        ResourcesText = SeasonEconomy.BalanceText(remainingCr, remainingSwc);
        foreach (var row in Rows())
        {
            row.IsAffordable = row.CostCr <= remainingCr && (double)(row.CostSwc ?? 0) <= remainingSwc;
        }

        OnPropertyChanged(nameof(HasCartItems));
        BuyCommand.NotifyCanExecuteChanged();
    }
}
