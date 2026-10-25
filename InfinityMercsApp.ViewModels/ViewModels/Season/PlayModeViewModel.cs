using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Inducements;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Presentation.Season;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.Services.Season.Inducements;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.ViewModels.Season.Inducements;
using InfinityMercsApp.ViewModels.Season.Inducements.Items;
using InfinityMercsApp.ViewModels.Season.Items;

namespace InfinityMercsApp.ViewModels.Season;

/// <summary>
/// Chooses which units deploy for a mission. The lieutenant always deploys; at most six slots can be
/// filled (a TAG Company TAG takes two), and four or fewer is an elite deployment. A Proxy Pack deploys any of the
/// Lone Wolf's loadouts, at least one, and picks the one the Lone Wolf starts in; the rest deploy as Aspects.
/// With the opponent's company renown, the underdog gets inducements; the company spends its own in the
/// inducements pop-up, and hired troops take the deployment up to seven troopers.
/// </summary>
public partial class PlayModeViewModel(
    INavigationService navigationService,
    IDialogService dialogs,
    DeploymentRoster roster,
    HiredTroops hiredTroops,
    IAppSettingsProvider? settings = null) : ViewModelBase(navigationService), IDeploymentChoice, IInducementShopOwner
{
    public const int MaxSlots = 6;
    public const int MaxEliteSlots = 4;

    private string _companyFilePath = string.Empty;
    private string? _seasonFilePath;
    private bool _loaded;
    private bool? _loadedInInches;

    public ObservableCollection<DeploymentUnitItem> DeploymentUnits { get; } = [];

    /// <summary>The deployed loadouts the Lone Wolf can start in.</summary>
    public ObservableCollection<DeploymentUnitItem> LoneWolfLoadouts { get; } = [];

    /// <summary>The loadout the Lone Wolf starts the game in.</summary>
    [ObservableProperty]
    public partial DeploymentUnitItem? StartingLoneWolf { get; set; }

    /// <summary>Whether the company is a Proxy Pack, which picks where its Lone Wolf starts.</summary>
    [ObservableProperty]
    public partial bool IsProxyPack { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedUnit), nameof(NoUnitSelected))]
    public partial DeploymentUnitItem? SelectedUnit { get; set; }

    public bool HasSelectedUnit => SelectedUnit is not null;

    public bool NoUnitSelected => SelectedUnit is null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEliteDeployment), nameof(DeploymentStatusText))]
    public partial int DeployedSlotCount { get; set; }

    /// <summary>Troops hired with inducements; they don't count toward the elite deployment's four.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DeploymentStatusText))]
    public partial int HiredCount { get; set; }

    public bool IsEliteDeployment => DeployedSlotCount is > 0 and <= MaxEliteSlots;

    public string DeploymentStatusText =>
        $"{DeployedSlotCount} / {MaxSlots} slots" +
        (HiredCount > 0 ? $" + {HiredCount} hired" : string.Empty) +
        (IsEliteDeployment ? " · ELITE" : string.Empty);

    /// <summary>The company's renown, as the season page shows it; whoever deploys, it is what inducements go by.</summary>
    [ObservableProperty]
    public partial int CompanyRenown { get; set; }

    /// <summary>The opponent company's renown as typed; blank until the opponent gives it.</summary>
    [ObservableProperty]
    public partial string OpponentRenownText { get; set; } = string.Empty;

    /// <summary>A cooperative contract, which gives neither side inducements.</summary>
    [ObservableProperty]
    public partial bool IsCooperative { get; set; }

    /// <summary>Who gets inducements, and how many.</summary>
    [ObservableProperty]
    public partial string InducementText { get; set; } = string.Empty;

    /// <summary>The company gets inducements, or still has something bought with them, so the pop-up opens.</summary>
    [ObservableProperty]
    public partial bool CanSpendInducements { get; set; }

    /// <summary>Both sides' deployed renown and what the company got and bought with its inducements.</summary>
    public ContractInducements Inducements { get; } = new();

    /// <summary>The inducements pop-up.</summary>
    public InducementShopViewModel Shop { get; } = new(dialogs, hiredTroops);

    partial void OnOpponentRenownTextChanged(string value) => RefreshInducements();

    partial void OnIsCooperativeChanged(bool value) => RefreshInducements();

    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (NavigationParameters.GetString(query, NavigationParameters.CompanyFilePath) is { } companyFilePath)
        {
            _companyFilePath = companyFilePath;
            _loaded = false;
        }

        _seasonFilePath = NavigationParameters.GetString(query, NavigationParameters.SeasonFilePath) ?? _seasonFilePath;
    }

    /// <summary>Loads the company once, and again if the distance units setting has changed since.</summary>
    public override async Task InitializeAsync()
    {
        var inches = SeasonDisplayUnitFormatter.GetShowUnitsInInches(settings);
        if ((_loaded && _loadedInInches == inches) || string.IsNullOrWhiteSpace(_companyFilePath))
        {
            return;
        }

        _loaded = true;
        _loadedInInches = inches;

        foreach (var unit in DeploymentUnits)
        {
            unit.Deployment = null;
        }

        DeploymentUnits.Clear();
        CompanyRenown = await roster.CompanyRenownAsync(_companyFilePath, _seasonFilePath);
        foreach (var unit in await roster.LoadAsync(_companyFilePath, _seasonFilePath, inches))
        {
            unit.Deployment = this;
            DeploymentUnits.Add(unit);
        }

        DeployedSlotCount = CountDeployedSlots();
        IsProxyPack = DeploymentUnits.Any(unit => unit.IsLoneWolf);
        RefreshLoneWolfLoadouts();
        SelectUnit(DeploymentUnits.FirstOrDefault());
        Shop.Owner = this;
        RefreshInducements();
    }

    [RelayCommand]
    private void SelectUnit(DeploymentUnitItem? item)
    {
        foreach (var unit in DeploymentUnits)
        {
            unit.IsSelected = ReferenceEquals(unit, item);
        }

        SelectedUnit = item;
    }

    [RelayCommand]
    private async Task DeployAsync()
    {
        var deployed = DeploymentUnits.Where(u => u.IsChecked).ToList();
        if (deployed.Count == 0)
        {
            await dialogs.ShowAlertAsync("No Units Selected", "Select at least one unit to deploy.");
            return;
        }

        if (InducementShop.Left(Inducements) < 0)
        {
            await dialogs.ShowAlertAsync(
                "Too Many Inducements Spent",
                $"This deployment gets {Inducements.Received} inducements, but {InducementShop.Spent(Inducements)} are spent. Return something first.");
            return;
        }

        var parameters = NavigationParameters.ForCompany(_companyFilePath, _seasonFilePath);
        parameters[NavigationParameters.DeployedIndices] = deployed.Select(u => u.EntryIndex).ToList();
        parameters[NavigationParameters.EliteDeployment] = IsEliteDeployment;
        parameters[NavigationParameters.Inducements] = Inducements;
        if (StartingLoneWolf is { } start)
        {
            parameters[NavigationParameters.LoneWolfStart] = start.EntryIndex;
        }

        await NavigationService.PushAsync("GameModePage", parameters);
    }

    // Undoes a check that would leave the lieutenant behind or go over the slot limit.
    /// <summary>Applies the deployment rules when a trooper is ticked or unticked.</summary>
    public void CheckedChanged(DeploymentUnitItem unit)
    {
        // The lieutenant always deploys; a Lone Wolf needs at least one of its loadouts deployed.
        if ((unit is { IsLieutenant: true, IsLoneWolf: false, IsChecked: false }) ||
            (unit is { IsPartOfLoneWolf: true, IsChecked: false } && !DeploymentUnits.Any(other => other is { IsPartOfLoneWolf: true, IsChecked: true })))
        {
            unit.IsChecked = true;
            return;
        }

        // Hired troops take the room up to seven troopers, but the company's own still fill at most six slots.
        if (unit.IsChecked &&
            (CountDeployedSlots() > MaxSlots || CountDeployedSlots() + Inducements.Hired.Count > InducementRules.TrooperLimitWithHired))
        {
            unit.IsChecked = false;
            return;
        }

        DeployedSlotCount = CountDeployedSlots();
        RefreshLoneWolfLoadouts();
        RefreshInducements();
    }

    /// <summary>Keeps the deployment page's hired count and inducement button up with the shop.</summary>
    public void ShopChanged()
    {
        HiredCount = Shop.HiredCount;
        CanSpendInducements = Inducements.Received > 0 || InducementShop.Spent(Inducements) > 0;
    }

    // The inducements the company's renown brings against the opponent's, and the pop-up's view of them. Gear rented
    // for a trooper no longer deploying goes back.
    private void RefreshInducements()
    {
        var deployed = DeploymentUnits.Where(u => u.IsChecked).ToList();
        InducementShop.KeepDeployed(Inducements, deployed.Select(u => u.EntryIndex).ToHashSet());
        var hasOpponentRenown = int.TryParse(OpponentRenownText, NumberStyles.Integer, CultureInfo.CurrentCulture, out var opponentRenown) &&
                                opponentRenown >= 0;
        Inducements.CompanyRenown = CompanyRenown;
        Inducements.OpponentRenown = hasOpponentRenown ? opponentRenown : 0;
        Inducements.IsCooperative = IsCooperative;
        Inducements.Received = hasOpponentRenown ? InducementRules.For(CompanyRenown, opponentRenown, IsCooperative) : 0;
        var opponentGets = hasOpponentRenown ? InducementRules.For(opponentRenown, CompanyRenown, IsCooperative) : 0;
        InducementText =
            !hasOpponentRenown ? "Enter the opponent's company renown for inducements."
            : IsCooperative ? "A cooperative contract gives no inducements."
            : Inducements.Received > 0 ? $"You get {Inducements.Received} inducements."
            : opponentGets > 0 ? $"Your opponent gets {opponentGets} inducements."
            : "No inducements: the renown is too close.";
        CanSpendInducements = Inducements.Received > 0 || InducementShop.Spent(Inducements) > 0;
        Shop.Update(
            Inducements,
            deployed.Select(u => new ContractTrooper(u.EntryIndex, u.Name)).ToList(),
            DeployedSlotCount);
    }

    // The deployed loadouts offered for the Lone Wolf's start, keeping the pick while it stays deployed; the Lone
    // Wolf's own loadout is the first choice otherwise.
    private void RefreshLoneWolfLoadouts()
    {
        var deployed = DeploymentUnits.Where(unit => unit is { IsPartOfLoneWolf: true, IsChecked: true }).ToList();
        var keep = StartingLoneWolf is { } current && deployed.Contains(current) ? current : null;
        LoneWolfLoadouts.Clear();
        foreach (var loadout in deployed)
        {
            LoneWolfLoadouts.Add(loadout);
        }

        StartingLoneWolf = keep ?? deployed.FirstOrDefault(unit => unit.IsLoneWolf) ?? deployed.FirstOrDefault();
    }

    private int CountDeployedSlots() => DeploymentUnits.Where(u => u.IsChecked).Sum(u => u.SlotCost);
}
