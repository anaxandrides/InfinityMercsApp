using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Domain.Season.Marketplace;
using InfinityMercsApp.Domain.Season.RecruitRolls;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Season.Overview.Items;
using InfinityMercsApp.ViewModels.Marketplaces.Items;
using InfinityMercsApp.ViewModels.Components;

namespace InfinityMercsApp.ViewModels.Season.Overview;

/// <summary>
/// The season's market tab: the stores open to the company (the Base Market, the War Market chosen for this
/// round, then the services), one store's stock at notoriety-adjusted prices, and a cart kept across stores.
/// What is left to spend is the season's CR less the cart; SWC is not spent, so the balance shows the
/// company's SWC. Buying records the cart where the season is shopping now (the latest round, or the initial
/// purchases before the first contract) and then calls <c>purchased</c>. Each copy of armour that rolls on a
/// recruit table rolls as it is bought, and keeps its result.
/// </summary>
public partial class SeasonMarketplaceViewModel(
    IStoreProvider storeProvider,
    ISeasonFileService seasonFiles,
    IDialogService dialogs,
    MarketplaceDetailViewModel detailPopup,
    RecruitRollPrompt rolls,
    Func<Task> purchased) : ObservableObject
{
    private readonly Dictionary<string, SeasonCartLine> _cart = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<Store> _openStores = [];
    private string? _seasonFilePath;
    private string _shownStoreName = string.Empty;
    private SeasonLedger _ledger;
    private int _companyNotoriety;
    private bool _choosingStore;

    [ObservableProperty]
    public partial IReadOnlyList<StoreChoice> Stores { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsServicesStore), nameof(IsRecruitmentStore), nameof(ShowsStock))]
    public partial StoreChoice? SelectedStore { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsStock))]
    public partial bool HasStore { get; set; }

    [ObservableProperty]
    public partial string StoreName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAlignmentText))]
    public partial string AlignmentText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<StoreItemGroup> ItemGroups { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTroopTypes))]
    public partial IReadOnlyList<TroopTypeRow> TroopTypeRows { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAugments))]
    public partial IReadOnlyList<AugmentRow> AugmentRows { get; set; } = [];

    /// <summary>The CR left once the cart is paid for, and the company's SWC, e.g. "12 CR - 1.5 SWC".</summary>
    [ObservableProperty]
    public partial string ResourcesText { get; set; } = SeasonEconomy.BalanceText(0, 0);

    /// <summary>The popup showing a row in full.</summary>
    public MarketplaceDetailViewModel DetailPopup => detailPopup;

    public bool HasAlignmentText => AlignmentText.Length > 0;

    /// <summary>Medical Services sells services rather than stock.</summary>
    public bool IsServicesStore => string.Equals(SelectedStore?.StoreName, ServiceStoreNames.MedicalServices, StringComparison.OrdinalIgnoreCase);

    /// <summary>Additional Recruitment hires new troopers rather than selling stock.</summary>
    public bool IsRecruitmentStore => string.Equals(SelectedStore?.StoreName, ServiceStoreNames.AdditionalRecruitment, StringComparison.OrdinalIgnoreCase);

    /// <summary>A store with stock is showing.</summary>
    public bool ShowsStock => HasStore && !IsServicesStore && !IsRecruitmentStore;
    public bool HasTroopTypes => TroopTypeRows.Count > 0;
    public bool HasAugments => AugmentRows.Count > 0;
    /// <summary>Something is in the cart, so it can be bought.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BuyCommand))]
    public partial bool HasCartItems { get; set; }

    /// <summary>The buy button's text, with what the cart holds across stores, e.g. "BUY · 3 items · 27 CR".</summary>
    [ObservableProperty]
    public partial string BuyText { get; set; } = "BUY";

    /// <summary>The markets of the company's factions, which it may take as its War Market without rolling.</summary>
    public IReadOnlyList<string> FactionMarketNames { get; private set; } = [];

    /// <summary>Takes in the season's balance and the company's notoriety.</summary>
    public void RefreshBalance(SeasonFile? seasonFile)
    {
        _ledger = SeasonEconomy.Ledger(seasonFile);
        _companyNotoriety = SeasonRounds.CompanyNotoriety(seasonFile);
        ShowAffordability();
    }

    /// <summary>
    /// Lists the stores open to a company of factions with these names: the Base Market, this round's War
    /// Market once chosen, then the services. Shows the first.
    /// </summary>
    public async Task LoadStoresAsync(IReadOnlySet<string> factionNames, string? seasonFilePath, SeasonFile? seasonFile)
    {
        _seasonFilePath = seasonFilePath;
        var available = await storeProvider.GetAvailableStoresAsync(factionNames.ToList());
        FactionMarketNames = WarMarkets.FactionMarkets(available, factionNames).Select(store => store.Name).ToList();

        List<Store> open = [.. available.Where(store => !store.IsServiceStore && !store.IsWarMarket)];
        if (WarMarkets.Current(seasonFile) is { } warMarketName && await storeProvider.GetStoreByNameAsync(warMarketName) is { } warMarket)
        {
            open.Add(warMarket);
        }

        // Medical Services, then Additional Recruitment, always last.
        open.AddRange(available
            .Where(store => store.IsServiceStore)
            .OrderBy(store => string.Equals(store.Name, ServiceStoreNames.MedicalServices, StringComparison.OrdinalIgnoreCase) ? 0 : 1));
        _openStores = open;

        List<StoreChoice> choices = [.. open.Select(store => new StoreChoice(StoreLabels.For(store), store.Name))];
        Stores = choices;
        _choosingStore = true;
        SelectedStore = choices.FirstOrDefault();
        _choosingStore = false;
        await SelectStoreAsync(SelectedStore);
    }

    /// <summary>The stores the company can shop at this round, by name.</summary>
    public IReadOnlyList<string> StoreNames() => _openStores.Select(store => store.Name).ToList();

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
        ShowAffordability();
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

    /// <summary>Records the cart where the season is shopping now (exchanges grant their SWC) and empties it.</summary>
    [RelayCommand(CanExecute = nameof(HasCartItems))]
    private async Task BuyAsync()
    {
        var ledger = SeasonEconomy.Ledger(await seasonFiles.LoadAsync(_seasonFilePath));
        var transactions = new List<SeasonTransaction>();
        foreach (var line in _cart.Values)
        {
            var table = RollTableOf(line);
            for (var copy = 0; copy < line.Quantity; copy++)
            {
                string? rolled = null;
                if (table is { } rollsOn)
                {
                    if (await rolls.RollAsync(line.ItemName, new OwedRoll(rollsOn, CanReroll: false), null, "Cancel") is not { } roll)
                    {
                        await dialogs.ShowAlertAsync("Nothing Bought", $"{line.ItemName} rolls on the {rollsOn} table as it is bought, so nothing was bought.");
                        return;
                    }

                    rolled = RollEffects.Side(roll.Result, null);
                }

                transactions.Add(new SeasonTransaction
                {
                    OriginStore = line.StoreName,
                    ItemName = line.ItemName,
                    CostCr = line.CostCr,
                    CostSwc = line.CostSwc > 0 ? line.CostSwc : null,
                    SwcGrant = string.Equals(line.Category, InventoryCategories.Exchange, StringComparison.OrdinalIgnoreCase)
                        ? SeasonEconomy.SwcGrantFromItemName(line.ItemName)
                        : null,
                    RolledResult = rolled
                });
            }
        }

        var storeNames = StoreNames().ToList();

        var recorded = await seasonFiles.UpdateAsync(_seasonFilePath, season =>
        {
            if (SeasonRounds.Latest(season) is { StartingCr: 0, StartingSwc: 0 } round)
            {
                round.StartingCr = ledger.CreditsBalance;
                round.StartingSwc = ledger.CompanySwc;
            }

            var marketplace = SeasonRounds.CurrentMarketplace(season);
            marketplace.Transactions.AddRange(transactions);
            marketplace.Stores = storeNames;
        });
        if (!recorded)
        {
            await dialogs.ShowAlertAsync("Nothing Bought", SeasonMessages.NotSaved);
            return;
        }

        _cart.Clear();
        foreach (var row in Rows())
        {
            row.Quantity = 0;
        }

        ShowAffordability();
        await purchased();
    }

    // The recruit table a line's armour rolls on when bought, if any.
    private RecruitRollTable? RollTableOf(SeasonCartLine line) =>
        _openStores
            .Where(store => string.Equals(store.Name, line.StoreName, StringComparison.OrdinalIgnoreCase))
            .SelectMany(store => store.TroopTypes)
            .FirstOrDefault(troop => !string.IsNullOrWhiteSpace(troop.ArmorName) &&
                                     string.Equals(InventoryCatalog.ArmorName(troop), line.ItemName, StringComparison.OrdinalIgnoreCase))
            ?.Effects.Roll;

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

        ShowAffordability();
    }

    private string CartKey(MarketplaceRow row) => $"{_shownStoreName}|{row.CartName}";

    private IEnumerable<MarketplaceRow> Rows() =>
        [.. ItemGroups.SelectMany(group => group.Items), .. TroopTypeRows, .. AugmentRows];

    // A row is affordable while one more fits in the CR left after the cart; SWC only limits equipping.
    private void ShowAffordability()
    {
        var cartCr = _cart.Values.Sum(line => line.TotalCr);
        var remainingCr = _ledger.CreditsBalance - cartCr;
        ResourcesText = SeasonEconomy.BalanceText(remainingCr, _ledger.CompanySwc);
        foreach (var row in Rows())
        {
            row.IsAffordable = row.CostCr <= remainingCr;
        }

        HasCartItems = _cart.Count > 0;
        var items = _cart.Values.Sum(line => line.Quantity);
        BuyText = HasCartItems ? $"BUY · {items} {(items == 1 ? "item" : "items")} · {cartCr} CR" : "BUY";
    }
}
