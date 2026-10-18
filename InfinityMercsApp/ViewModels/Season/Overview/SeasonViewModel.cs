using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Domain.Season.RecruitRolls;
using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Presentation.Company.Viewer;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.Recruitment;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Rolls;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.Services.Season.Peripherals;
using InfinityMercsApp.Services.Stores;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.ViewModels.Components;

namespace InfinityMercsApp.ViewModels.Season.Overview;

/// <summary>
/// A company's season: its troopers and their gear, its inventory and the markets it can buy from, under a
/// title bar with the round and what is left to spend. Opening a company without a season file starts one.
/// Each time the page appears the season file is read again, since the round pages change it.
/// </summary>
public partial class SeasonViewModel : ViewModelBase, ITeamChanges
{
    /// <summary>What buying a peripheral for the Peripheral (Controller) augment is called in the marketplace.</summary>
    public const string PeripheralPrefix = "Peripheral: ";

    /// <summary>What selling one is called.</summary>
    public const string PeripheralSalePrefix = "Peripheral sold: ";

    public const string DefaultName = "Season";

    private readonly ICompanyFileService _companyFiles;
    private readonly ISeasonFileService _seasonFiles;
    private readonly StoreCatalog _stores;
    private readonly IArmyDataService _armyData;
    private readonly IDialogService _dialogs;
    private readonly ICompanySelectionNavigator _companySelection;
    private readonly RecruitRollPrompt _rolls;
    private readonly TohaaPeripherals _peripherals;
    private readonly CompanyRoster _roster;
    private readonly HashSet<int> _rollsToAskFor = [];
    private string _companyFilePath = string.Empty;
    private string? _seasonFilePath;
    private bool _needsLoad;
    private SavedCompanyFile? _company;

    public SeasonViewModel(
        INavigationService navigationService,
        ICompanyFileService companyFiles,
        ISeasonFileService seasonFiles,
        IStoreProvider storeProvider,
        StoreCatalog stores,
        IArmyDataService armyData,
        IDialogService dialogs,
        ICompanySelectionNavigator companySelection,
        IDice dice,
        IAppPackageFiles packageFiles,
        CompanyRoster roster,
        UnitDetailViewModel detail,
        MarketplaceDetailViewModel detailPopup) : base(navigationService)
    {
        _companyFiles = companyFiles;
        _seasonFiles = seasonFiles;
        _stores = stores;
        _armyData = armyData;
        _dialogs = dialogs;
        _companySelection = companySelection;
        DetailPopup = detailPopup;
        _rolls = new RecruitRollPrompt(dialogs, dice, armyData);
        _peripherals = new TohaaPeripherals(armyData);
        _roster = roster;
        Team = new SeasonTeamViewModel(roster, detail, packageFiles, seasonFiles, detailPopup, dialogs, RefreshSeasonAsync, this);
        Inventory = new SeasonInventoryViewModel(seasonFiles, dialogs, RefreshSeasonAsync);
        Marketplace = new SeasonMarketplaceViewModel(storeProvider, seasonFiles, dialogs, detailPopup, _rolls, RefreshSeasonAsync);
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
    public partial string RoundTitle { get; set; } = DefaultName;

    [ObservableProperty]
    public partial string CompanyName { get; set; } = DefaultName;

    [ObservableProperty]
    public partial string CompanyType { get; set; } = string.Empty;

    /// <summary>The company's renown: its troopers' costs, gear and XP, and its victories.</summary>
    [ObservableProperty]
    public partial int Renown { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTeamTab), nameof(IsInventoryTab), nameof(IsMarketplaceTab))]
    public partial SeasonTab ActiveTab { get; set; } = SeasonTab.Team;

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

    /// <summary>
    /// Loads the company the first time, then brings everything up to date with the season file. Coming back,
    /// the company is read again, since recruiting adds troopers to it. A company just founded, and recruits just
    /// hired, then roll for Booty and Metachemistry.
    /// </summary>
    public override async Task InitializeAsync()
    {
        Team.RefreshDisplayUnits();
        if (_needsLoad)
        {
            _needsLoad = false;
            await LoadAsync();
        }
        else if (_company is not null)
        {
            await ReloadCompanyAsync();
        }

        await RefreshSeasonAsync();
        var newTroopers = _rollsToAskFor.ToList();
        _rollsToAskFor.Clear();
        await RollOwedAsync(newTroopers);
    }

    private async Task ReloadCompanyAsync()
    {
        try
        {
            if (_companyFiles.Exists(_companyFilePath) && await _companyFiles.LoadAsync(_companyFilePath) is { } company)
            {
                var known = _company?.Entries.Select(entry => entry.EntryIndex).ToHashSet() ?? [];
                _rollsToAskFor.UnionWith(company.Entries.Select(entry => entry.EntryIndex).Where(index => !known.Contains(index)));
                _company = company;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Reading the season's company again failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Opens the company's selection screen to hire new troopers with the CR the season has left; a company
    /// type without one says so.
    /// </summary>
    [RelayCommand]
    private async Task RecruitAsync()
    {
        if (_company is not { } company || _seasonFilePath is not { } seasonFilePath)
        {
            return;
        }

        var season = await _seasonFiles.LoadAsync(seasonFilePath);
        var credits = SeasonEconomy.Ledger(season).CreditsBalance;
        var retired = season?.Retired.Select(trooper => trooper.EntryIndex).ToList() ?? [];
        if (!await _companySelection.OpenRecruitmentAsync(new CompanyRecruitment(_companyFilePath, seasonFilePath, company, credits, retired)))
        {
            await _dialogs.ShowAlertAsync("Can't Recruit", $"There is no recruitment screen for a {company.CompanyType}.");
        }
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

    /// <summary>Opens the bye round page, for a contract the company cannot play.</summary>
    [RelayCommand]
    private Task TakeByeAsync() =>
        NavigationService.PushAsync("ByeRoundPage", NavigationParameters.ForCompany(_companyFilePath, _seasonFilePath ?? string.Empty));

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
            if (_seasonFilePath is null)
            {
                // A company opened without a season was just founded.
                _seasonFilePath = await _seasonFiles.CreateAsync(CompanyName, company.CompanyIdentifier, _companyFilePath);
                _rollsToAskFor.UnionWith(company.Entries.Select(entry => entry.EntryIndex));
            }

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
        var stores = await _stores.AllAsync();
        var catalog = await _stores.CatalogAsync();
        if (_company is { } company)
        {
            await Team.LoadAsync(company, _seasonFilePath, seasonFile, catalog);
        }

        var currentRound = SeasonRounds.Current(seasonFile);
        RoundTitle = currentRound == 0 ? "Round 0" : $"Round {currentRound} Marketplace";

        Renown = _company is { } renowned ? SeasonRenown.OfCompany(renowned, seasonFile, catalog, _roster.ServantsOf(renowned)) : 0;
        var inventory = SeasonInventory.From(seasonFile, catalog);
        Marketplace.RefreshBalance(seasonFile);
        Inventory.Show(_seasonFilePath, seasonFile, inventory);
        Services.Show(_seasonFilePath, seasonFile, Team.Units.ToList());
        Team.ShowOwnedGear(inventory, catalog, SeasonEconomy.Ledger(seasonFile).CompanySwc);
        WarMarketChoice.Prepare(
            _seasonFilePath,
            seasonFile,
            Marketplace.FactionMarketNames,
            stores.Where(store => store.IsWarMarket).Select(store => store.Name).ToList());
    }

    // The Team tab's roll button, for a trooper still owed a roll.
    Task ITeamChanges.RollOwedAsync(int entryIndex) => RollOwedAsync([entryIndex]);

    async Task<bool> ITeamChanges.ChangePeripheralsAsync(PeripheralChange change)
    {
        if (await LoadCompanyFileAsync() is not { } company)
        {
            return false;
        }

        var season = await _seasonFiles.LoadAsync(_seasonFilePath);
        foreach (var peripheral in company.Entries.Where(entry =>
                     entry.FromControllerAugment && entry.ParentEntryIndex is { } controller && change.LosingControl.Contains(controller)))
        {
            peripheral.ParentEntryIndex = null;
        }

        PeripheralOffer? bought = null;
        if (change.GainingControl is { } gaining)
        {
            var owned = company.Entries
                .Where(entry => entry.FromControllerAugment && SeasonTroopers.ControllerOf(entry, season) is null && season?.IsRetired(entry.EntryIndex) != true)
                .ToList();
            var offers = _peripherals.Offers();
            List<string> choices = [.. owned.Select(OwnedChoice), .. offers.Select(offer => offer.Display)];
            if (choices.Count == 0)
            {
                await _dialogs.ShowAlertAsync("No Peripherals", "The army data has no Tohaa peripherals to choose from; update the data and try again.");
                return false;
            }

            var choice = await _dialogs.ChooseAsync($"Peripheral for {change.GainingName}", "Cancel", choices);
            if (owned.FirstOrDefault(entry => OwnedChoice(entry) == choice) is { } reattached)
            {
                reattached.ParentEntryIndex = gaining;
            }
            else if (offers.FirstOrDefault(offer => offer.Display == choice) is { } offer)
            {
                var credits = SeasonEconomy.Ledger(season).CreditsBalance;
                var nextIndex = company.Entries.Count == 0 ? 0 : company.Entries.Max(entry => entry.EntryIndex) + 1;
                if (offer.Cost > credits)
                {
                    await _dialogs.ShowAlertAsync("Not Enough CR", $"{offer.Name} costs {offer.Cost} CR; the company has {credits} CR.");
                    return false;
                }

                if (_peripherals.EntryFor(offer, nextIndex, gaining) is not { } added)
                {
                    return false;
                }

                company.Entries.Add(added);
                bought = offer;
            }
            else
            {
                return false;
            }
        }

        if (!await SaveCompanyFileAsync(company))
        {
            return false;
        }

        if (bought is { } paidFor && !await _seasonFiles.UpdateAsync(_seasonFilePath, seasonFile =>
                SeasonRounds.CurrentMarketplace(seasonFile).Transactions.Add(new SeasonTransaction
                {
                    OriginStore = ServiceStoreNames.AdditionalRecruitment,
                    ItemName = PeripheralPrefix + paidFor.Name,
                    CostCr = paidFor.Cost
                })))
        {
            company.Entries.RemoveAt(company.Entries.Count - 1);
            await SaveCompanyFileAsync(company);
            await _dialogs.ShowAlertAsync("Peripheral Not Bought", SeasonMessages.NotSaved);
            return false;
        }

        _company = company;
        return true;

        static string OwnedChoice(SavedCompanyEntry entry) => $"{SeasonTroopers.NameOf(entry)} (owned, #{entry.EntryIndex})";
    }

    async Task ITeamChanges.SellPeripheralAsync(int entryIndex)
    {
        if (await LoadCompanyFileAsync() is not { } company ||
            company.Entries.FirstOrDefault(entry => entry.EntryIndex == entryIndex && entry.FromControllerAugment) is not { } peripheral)
        {
            return;
        }

        var name = SeasonTroopers.NameOf(peripheral);
        var price = (int)Math.Ceiling(peripheral.Cost / 2.0);
        if (!await _dialogs.ConfirmAsync("Sell Peripheral", $"Sell {name} for {price} CR? It leaves the company, with its XP and perks.", "Sell", "Cancel"))
        {
            return;
        }

        company.Entries.Remove(peripheral);
        if (!await SaveCompanyFileAsync(company))
        {
            return;
        }

        if (!await _seasonFiles.UpdateAsync(_seasonFilePath, seasonFile =>
                SeasonRounds.CurrentMarketplace(seasonFile).Transactions.Add(new SeasonTransaction
                {
                    OriginStore = ServiceStoreNames.AdditionalRecruitment,
                    ItemName = PeripheralSalePrefix + name,
                    CostCr = -price,
                    IsSale = true
                })))
        {
            company.Entries.Add(peripheral);
            await SaveCompanyFileAsync(company);
            await _dialogs.ShowAlertAsync("Peripheral Not Sold", SeasonMessages.NotSaved);
            return;
        }

        _company = company;
        await RefreshSeasonAsync();
    }

    private async Task<SavedCompanyFile?> LoadCompanyFileAsync()
    {
        try
        {
            return await _companyFiles.LoadAsync(_companyFilePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            Console.Error.WriteLine($"Reading the company failed: {ex.Message}");
            await _dialogs.ShowAlertAsync("Company Not Read", "The company file couldn't be read.");
            return null;
        }
    }

    private async Task<bool> SaveCompanyFileAsync(SavedCompanyFile company)
    {
        try
        {
            await _companyFiles.SaveAsync(_companyFilePath, company);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Saving the company failed: {ex.Message}");
            await _dialogs.ShowAlertAsync("Company Not Saved", "The company file couldn't be saved, so nothing changed.");
            return false;
        }
    }

    // Each trooper rolls what it owes in turn, and each roll is saved to the company as it is made. Putting one off
    // stops the rest; the Team tab offers them again.
    private async Task RollOwedAsync(IReadOnlyList<int> entryIndexes)
    {
        var rolledAny = false;
        foreach (var unit in Team.Units.Where(unit => entryIndexes.Contains(unit.EntryIndex)).ToList())
        {
            foreach (var owed in unit.OwedRolls)
            {
                if (await _rolls.RollAsync(unit.Name, owed, unit.UnitTypeCode, "Later") is not { } roll)
                {
                    await AfterRollsAsync(rolledAny);
                    return;
                }

                if (!await SaveRollAsync(unit.EntryIndex, roll))
                {
                    await _dialogs.ShowAlertAsync("Roll Not Saved", $"{unit.Name}'s {owed.Table} roll couldn't be saved to the company file.");
                    await AfterRollsAsync(rolledAny);
                    return;
                }

                rolledAny = true;
            }
        }

        await AfterRollsAsync(rolledAny);
    }

    private async Task AfterRollsAsync(bool rolledAny)
    {
        if (rolledAny)
        {
            await ReloadCompanyAsync();
            await RefreshSeasonAsync();
        }
    }

    private async Task<bool> SaveRollAsync(int entryIndex, RecruitRoll roll)
    {
        try
        {
            if (await _companyFiles.LoadAsync(_companyFilePath) is not { } company ||
                company.Entries.FirstOrDefault(entry => entry.EntryIndex == entryIndex) is not { } entry)
            {
                return false;
            }

            entry.RecruitRolls.Add(roll);
            await _companyFiles.SaveAsync(_companyFilePath, company);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            Console.Error.WriteLine($"Saving a recruit roll failed: {ex.Message}");
            return false;
        }
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
