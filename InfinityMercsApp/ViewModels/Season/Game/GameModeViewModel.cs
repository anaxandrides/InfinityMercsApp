using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Experience;
using InfinityMercsApp.Domain.Season.Game;
using InfinityMercsApp.Domain.Season.Injuries;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Providers.Metadata;
using InfinityMercsApp.Presentation.Season;
using InfinityMercsApp.Presentation.Season.Game;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.ViewModels.Season.Game.Items;

namespace InfinityMercsApp.ViewModels.Season.Game;

/// <summary>
/// The game in progress: the round, each kind of order token spent and left, and every deployed trooper's
/// wounds, XP and weapons. Ending the game records the mission and moves on to injuries or the outcome.
/// </summary>
public partial class GameModeViewModel(
    INavigationService navigationService,
    IDialogService dialogs,
    ISeasonFileService seasonFiles,
    DeploymentRoster roster,
    MissionDebrief debrief,
    IAppSettingsProvider? settings = null,
    IMetadataProvider? metadata = null) : ViewModelBase(navigationService)
{
    /// <summary>The game can be ended from this round on.</summary>
    public const int FirstEndableRound = 3;

    /// <summary>A game lasts at most this many rounds.</summary>
    public const int LastRound = 4;

    private string _companyFilePath = string.Empty;
    private string _seasonFilePath = string.Empty;
    private string? _loadedFilePath;
    private IReadOnlySet<int>? _deployedEntryIndices;
    private int? _loneWolfStart;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RoundText), nameof(CanEndGame), nameof(HasNextRound))]
    [NotifyCanExecuteChangedFor(nameof(NextRoundCommand), nameof(EndGameCommand))]
    public partial int CurrentRound { get; set; } = 1;

    [ObservableProperty]
    public partial bool IsEliteDeployment { get; set; }

    [ObservableProperty]
    public partial bool ShowUnitsInInches { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsShowingWeapon))]
    public partial GameWeaponCard? OpenWeaponCard { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsShowingXp))]
    public partial XpSheet? OpenXpSheet { get; set; }

    public ObservableCollection<GameUnitItem> Units { get; } = [];

    /// <summary>Lieutenant, impetuous, irregular and regular orders, in the order they are shown.</summary>
    public IReadOnlyList<OrderPoolItem> Orders { get; } =
    [
        new(OrderKind.Lieutenant),
        new(OrderKind.Impetuous),
        new(OrderKind.Irregular),
        new(OrderKind.Regular)
    ];

    public string RoundText => $"Round {CurrentRound}";

    public bool CanEndGame => CurrentRound >= FirstEndableRound;

    public bool HasNextRound => CurrentRound < LastRound;

    public bool IsShowingWeapon => OpenWeaponCard is not null;

    public bool IsShowingXp => OpenXpSheet is not null;

    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _companyFilePath = NavigationParameters.GetString(query, NavigationParameters.CompanyFilePath) ?? _companyFilePath;
        _seasonFilePath = NavigationParameters.GetString(query, NavigationParameters.SeasonFilePath) ?? _seasonFilePath;
        _deployedEntryIndices = NavigationParameters.GetIntSet(query, NavigationParameters.DeployedIndices);
        _loneWolfStart = NavigationParameters.GetInt(query, NavigationParameters.LoneWolfStart);
        IsEliteDeployment = NavigationParameters.GetFlag(query, NavigationParameters.EliteDeployment);
    }

    /// <summary>Loads the deployed troopers the first time the page appears for a company.</summary>
    public override async Task InitializeAsync()
    {
        ShowUnitsInInches = SeasonDisplayUnitFormatter.GetShowUnitsInInches(settings);
        if (_loadedFilePath == _companyFilePath || string.IsNullOrWhiteSpace(_companyFilePath))
        {
            return;
        }

        _loadedFilePath = _companyFilePath;
        Units.Clear();
        CurrentRound = 1;

        try
        {
            var weapons = new GameWeaponCatalog(metadata);
            var troopers = await roster.LoadAsync(_companyFilePath, _seasonFilePath, ShowUnitsInInches, _deployedEntryIndices);
            foreach (var trooper in troopers)
            {
                var skills = AttackModifiers.NormalizeSkills(trooper.Skills);
                var modifiers = AttackModifiers.From(skills, trooper.Equipment);
                var hasTacticalToken = EliteDeployment.HasTacticalAwareness(
                    DeploymentRoster.HasLine(trooper.Skills, "Tactical Awareness"), trooper.IsLieutenant, IsEliteDeployment);
                Units.Add(new GameUnitItem(trooper, hasTacticalToken, weapons.CardsFor(trooper.RangedWeapons, trooper.MeleeWeapons, modifiers)));
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Loading the game failed: {ex.Message}");
        }

        PlaceLoneWolf();
        ResetOrders();
    }

    // A Proxy Pack's Lone Wolf starts in the loadout picked at deployment, else its own, else the first deployed.
    private void PlaceLoneWolf()
    {
        var loadouts = Units.Where(unit => unit.IsPartOfLoneWolf).ToList();
        var start = loadouts.FirstOrDefault(unit => unit.Unit.EntryIndex == _loneWolfStart)
                    ?? loadouts.FirstOrDefault(unit => unit.Unit.IsLoneWolf)
                    ?? loadouts.FirstOrDefault();
        foreach (var loadout in loadouts)
        {
            loadout.HostsLoneWolf = ReferenceEquals(loadout, start);
        }
    }

    // ── Rounds and orders ───────────────────────────────────────────────────

    /// <summary>Starts the next round: fresh orders from the troopers still standing, and Tactical Awareness tokens back.</summary>
    [RelayCommand(CanExecute = nameof(HasNextRound))]
    private void NextRound()
    {
        CurrentRound++;
        ResetOrders();
        foreach (var unit in Units)
        {
            unit.ResetTacticalToken();
        }
    }

    private void ResetOrders()
    {
        var counts = OrderCounts.For(Units.Select(unit => unit.OrderSource));
        foreach (var pool in Orders)
        {
            pool.Reset(counts.Of(pool.Kind));
        }
    }

    /// <summary>
    /// Transfer Consciousness: the Lone Wolf moves into a standing Aspect, which takes its profile; where it was, an
    /// Aspect is left with the wounds and states. The player checks the skill's requirements.
    /// </summary>
    [RelayCommand]
    private void TransferConsciousness(GameUnitItem target)
    {
        if (!target.CanReceiveLoneWolf || Units.FirstOrDefault(unit => unit.HostsLoneWolf) is not { } current)
        {
            return;
        }

        current.HostsLoneWolf = false;
        target.HostsLoneWolf = true;
    }

    // ── Accordion and pop-ups ───────────────────────────────────────────────

    /// <summary>Opens a trooper's row and closes the others, or closes it when it is already open.</summary>
    [RelayCommand]
    private void ToggleUnit(GameUnitItem unit)
    {
        var expand = !unit.IsExpanded;
        foreach (var other in Units)
        {
            other.IsExpanded = false;
        }

        unit.IsExpanded = expand;
    }

    [RelayCommand]
    private void ShowWeapon(GameWeaponCard card)
    {
        if (card.IsMatched)
        {
            OpenWeaponCard = card;
        }
    }

    [RelayCommand]
    private void CloseWeapon() => OpenWeaponCard = null;

    [RelayCommand]
    private void ShowXp(GameUnitItem unit) => OpenXpSheet = unit.XpSheet();

    [RelayCommand]
    private void CloseXp() => OpenXpSheet = null;

    // ── Ending the game ─────────────────────────────────────────────────────

    /// <summary>
    /// After confirming, records the mission on a new season round, hands every trooper's result to the
    /// debrief, and goes to injury checks when anyone died, otherwise to the mission outcome.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEndGame))]
    private async Task EndGameAsync()
    {
        if (!await dialogs.ConfirmAsync("End Game", "Are you sure you want to end the game?"))
        {
            return;
        }

        var seasonFile = await seasonFiles.LoadAsync(_seasonFilePath);
        var missionNumber = SeasonRounds.Current(seasonFile) + 1;
        if (seasonFile is not null)
        {
            seasonFile.Rounds.Add(new SeasonRound
            {
                RoundIndex = missionNumber,
                MissionResults = new SeasonMissionResult
                {
                    UnitsDeployed = Units.Count,
                    EliteDeploymentMet = IsEliteDeployment,
                    MissionRound = CurrentRound
                }
            });
            await seasonFiles.SaveAsync(_seasonFilePath, seasonFile);
        }

        debrief.Units = Units.Select(unit => unit.Unit.ToResult(missionNumber, IsEliteDeployment, wasDeployed: true)).ToList();
        debrief.Troopers = await CompanyTroopersAsync(debrief.Units);
        // Only the Lone Wolf checks for injury, wherever it ended; its Aspects never do. It goes by its own name even
        // when it ended in an Aspect and its own loadout stayed home.
        var troopers = debrief.Troopers;
        debrief.PendingInjuries = Units
            .Where(unit => unit.ChecksForInjury)
            .Select(unit => unit.Unit)
            .Where(unit => unit.WoundState == WoundState.Dead)
            .Select(unit => new PendingInjury
            {
                EntryIndex = unit.AspectOf ?? unit.EntryIndex,
                Name = unit.AspectOf is { } wolf && troopers.FirstOrDefault(trooper => trooper.EntryIndex == wolf) is { } loneWolf
                    ? loneWolf.Name
                    : unit.Name,
                PhValue = unit.UnitPh,
                CachedLogoPath = unit.CachedLogoPath,
                PackagedLogoPath = unit.PackagedLogoPath
            })
            .ToList();

        await NavigationService.PushAsync(
            debrief.PendingInjuries.Count > 0 || SomeoneRecovers(seasonFile) ? "InjuriesPage" : "MissionOutcomePage",
            NavigationParameters.ForCompany(_companyFilePath, _seasonFilePath));
    }

    // Every active trooper, for downtime: the deployed ones as they finished the game, the rest as they stand. A Lone
    // Wolf's Aspects are the Lone Wolf, so they don't take part on their own.
    private async Task<List<ExperienceUnitResult>> CompanyTroopersAsync(IReadOnlyList<ExperienceUnitResult> deployed)
    {
        var company = await roster.LoadAsync(_companyFilePath, _seasonFilePath, ShowUnitsInInches);
        return company
            .Where(trooper => trooper.AspectOf is null)
            .Select(trooper => deployed.FirstOrDefault(result => result.EntryIndex == trooper.EntryIndex)
                               ?? trooper.ToResult(deploymentXp: 0, isEliteDeployment: false, wasDeployed: false))
            .ToList();
    }

    // An injured trooper that sat this contract out, or one the captain's healing can reach, sheds an injury on the
    // injuries page.
    private bool SomeoneRecovers(SeasonFile? seasonFile) =>
        seasonFile?.Troopers.Any(trooper =>
        {
            var deployed = Units.FirstOrDefault(unit => unit.Unit.EntryIndex == trooper.Key)?.Unit;
            var isCaptain = deployed?.IsLieutenant ?? false;
            var injured = trooper.Value.Injuries.Count > 0;
            return InjuryRules.Recovers(deployed is not null, isCaptain, injured) ||
                   InjuryRules.CanTakeCaptainsHealing(isCaptain, deployed is not null, IsEliteDeployment, injured);
        }) ?? false;
}
