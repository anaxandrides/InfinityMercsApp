using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Services;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.ViewModels.Company.Viewer;
using InfinityMercsApp.ViewModels.Marketplaces;
using InfinityMercsApp.ViewModels.UnitBrowser;

namespace InfinityMercsApp.ViewModels.Season.Overview;

/// <summary>
/// A company's season: its troopers and their gear, its inventory and the markets it can buy from, under a
/// title bar with the round and what is left to spend. Opening a company without a season file starts one.
/// Each time the page appears the season file is read again, since the round pages change it.
/// </summary>
public partial class SeasonViewModel : ViewModelBase
{
    public const string DefaultName = "Season";

    private readonly ICompanyFileService _companyFiles;
    private readonly ISeasonFileService _seasonFiles;
    private readonly IStoreProvider _storeProvider;
    private readonly IArmyDataService _armyData;
    private string _companyFilePath = string.Empty;
    private string? _seasonFilePath;
    private bool _needsLoad;
    private IReadOnlyList<Store>? _allStores;
    private InventoryCatalog _catalog = InventoryCatalog.Empty;
    private SavedCompanyFile? _company;

    public SeasonViewModel(
        INavigationService navigationService,
        ICompanyFileService companyFiles,
        ISeasonFileService seasonFiles,
        IStoreProvider storeProvider,
        IArmyDataService armyData,
        IDialogService dialogs,
        IDice dice,
        IAppPackageFiles packageFiles,
        CompanyRoster roster,
        UnitDetailViewModel detail,
        MarketplaceDetailViewModel detailPopup) : base(navigationService)
    {
        _companyFiles = companyFiles;
        _seasonFiles = seasonFiles;
        _storeProvider = storeProvider;
        _armyData = armyData;
        DetailPopup = detailPopup;
        Team = new SeasonTeamViewModel(roster, detail, packageFiles, seasonFiles, detailPopup, dialogs);
        Inventory = new SeasonInventoryViewModel(seasonFiles, dialogs, RefreshSeasonAsync);
        Marketplace = new SeasonMarketplaceViewModel(storeProvider, seasonFiles, dialogs, detailPopup, RefreshSeasonAsync);
        WarMarketChoice = new WarMarketChoiceViewModel(seasonFiles, dice, OnWarMarketChosenAsync);
        Services = new SeasonServicesViewModel(seasonFiles, dialogs, RefreshSeasonAsync);
    }

    public SeasonTeamViewModel Team { get; }

    public SeasonInventoryViewModel Inventory { get; }

    public SeasonMarketplaceViewModel Marketplace { get; }

    public WarMarketChoiceViewModel WarMarketChoice { get; }

    /// <summary>The services, shown when the Medical Services store is picked.</summary>
    public SeasonServicesViewModel Services { get; }

    /// <summary>The popup showing a weapon or market row in full.</summary>
    public MarketplaceDetailViewModel DetailPopup { get; }

    /// <summary>"Round 0" before the first round, then "Round N Marketplace".</summary>
    [ObservableProperty]
    private string _roundTitle = DefaultName;

    [ObservableProperty]
    private string _companyName = DefaultName;

    [ObservableProperty]
    private string _companyType = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTeamTab), nameof(IsInventoryTab), nameof(IsMarketplaceTab))]
    private SeasonTab _activeTab = SeasonTab.Team;

    public bool IsTeamTab => ActiveTab == SeasonTab.Team;
    public bool IsInventoryTab => ActiveTab == SeasonTab.Inventory;
    public bool IsMarketplaceTab => ActiveTab == SeasonTab.Marketplace;

    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (NavigationParameters.GetString(query, NavigationParameters.CompanyFilePath) is not { } companyFilePath)
        {
            return;
        }

        // A season being continued names its file; otherwise one is started when the company loads.
        _companyFilePath = companyFilePath;
        _seasonFilePath = NavigationParameters.GetString(query, NavigationParameters.SeasonFilePath);
        _needsLoad = true;
    }

    /// <summary>Loads the company the first time, then brings everything up to date with the season file.</summary>
    public override async Task InitializeAsync()
    {
        Team.RefreshDisplayUnits();
        if (_needsLoad)
        {
            _needsLoad = false;
            await LoadAsync();
        }

        await RefreshSeasonAsync();
    }

    /// <summary>Shows a tab; the market tab asks for this round's War Market when none is chosen yet.</summary>
    [RelayCommand]
    private async Task ShowTabAsync(SeasonTab tab)
    {
        ActiveTab = tab;
        if (tab == SeasonTab.Team)
        {
            return;
        }

        await RefreshSeasonAsync();
        if (tab == SeasonTab.Marketplace)
        {
            WarMarketChoice.OpenCommand.Execute(null);
        }
    }

    /// <summary>Opens the rank-up page to spend troopers' perk points.</summary>
    [RelayCommand]
    private Task RankUpAsync() =>
        NavigationService.PushAsync("RankUpPage", NavigationParameters.ForCompany(_companyFilePath, _seasonFilePath ?? string.Empty));

    [RelayCommand]
    private Task PlayRoundAsync() =>
        NavigationService.PushAsync("PlayModePage", NavigationParameters.ForCompany(_companyFilePath, _seasonFilePath ?? string.Empty));

    private async Task LoadAsync()
    {
        try
        {
            if (!_companyFiles.Exists(_companyFilePath) || await _companyFiles.LoadAsync(_companyFilePath) is not { } company)
            {
                ShowNoCompany();
                return;
            }

            CompanyName = string.IsNullOrWhiteSpace(company.CompanyName)
                ? Path.GetFileNameWithoutExtension(_companyFilePath)
                : company.CompanyName;
            CompanyType = company.CompanyType;
            _seasonFilePath ??= await _seasonFiles.CreateAsync(CompanyName, company.CompanyIdentifier, _companyFilePath);

            // The CR left from recruiting and the captain's SWC, for a season that does not have them yet.
            if (await _seasonFiles.LoadAsync(_seasonFilePath) is { StartingFunds: null })
            {
                await _seasonFiles.UpdateAsync(_seasonFilePath, season => season.StartingFunds ??= SeasonStart.FundsFor(company));
            }

            var seasonFile = await _seasonFiles.LoadAsync(_seasonFilePath);
            _company = company;
            Marketplace.RefreshBalance(seasonFile);
            await Marketplace.LoadStoresAsync(FactionNames(company), _seasonFilePath, seasonFile);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Loading the season's company failed: {ex.Message}");
            ShowNoCompany();
        }
    }

    private void ShowNoCompany()
    {
        _company = null;
        CompanyName = DefaultName;
        CompanyType = string.Empty;
        Team.Clear();
    }

    // The company's factions and their vanilla parents, which decide the markets open to it.
    private IReadOnlySet<string> FactionNames(SavedCompanyFile company)
    {
        var allFactions = _armyData.GetMetadataFactions();
        var companyFactions = company.SourceFactions
            .Select(faction => _armyData.GetMetadataFactionById(faction.FactionId))
            .OfType<Faction>()
            .ToList();
        return FactionResolver.GetExpandedFactionNames(companyFactions, allFactions);
    }

    // Re-reads the season: the round, the troopers, the balance, what is owned, and whether a War Market is wanted.
    private async Task RefreshSeasonAsync()
    {
        var seasonFile = await _seasonFiles.LoadAsync(_seasonFilePath);
        if (_company is { } company)
        {
            await Team.LoadAsync(company, _seasonFilePath, seasonFile);
        }

        var currentRound = SeasonRounds.Current(seasonFile);
        RoundTitle = currentRound == 0 ? "Round 0" : $"Round {currentRound} Marketplace";

        var stores = await AllStoresAsync();
        var inventory = SeasonInventory.From(seasonFile, _catalog);
        Marketplace.RefreshBalance(seasonFile);
        Inventory.Show(_seasonFilePath, seasonFile, inventory);
        Services.Show(_seasonFilePath, seasonFile, Team.Units.ToList());
        Team.ShowOwnedGear(inventory, _catalog, SeasonEconomy.Ledger(seasonFile).CompanySwc);
        WarMarketChoice.Prepare(
            _seasonFilePath,
            seasonFile,
            Marketplace.FactionMarketNames,
            stores.Where(store => store.IsWarMarket).Select(store => store.Name).ToList());
    }

    // Every store, read once; the catalog of their stock gives owned items their category, price and SWC.
    private async Task<IReadOnlyList<Store>> AllStoresAsync()
    {
        if (_allStores is null)
        {
            List<Store> stores = [];
            foreach (var name in _storeProvider.GetAllStoreNames())
            {
                if (await _storeProvider.GetStoreByNameAsync(name) is { } store)
                {
                    stores.Add(store);
                }
            }

            _allStores = stores;
            _catalog = InventoryCatalog.From(stores);
        }

        return _allStores;
    }

    // The new War Market joins the market list straight away.
    private async Task OnWarMarketChosenAsync(string storeName)
    {
        if (!_companyFiles.Exists(_companyFilePath) || await _companyFiles.LoadAsync(_companyFilePath) is not { } company)
        {
            return;
        }

        await Marketplace.LoadStoresAsync(FactionNames(company), _seasonFilePath, await _seasonFiles.LoadAsync(_seasonFilePath));
        await RefreshSeasonAsync();
    }
}
