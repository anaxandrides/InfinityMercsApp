using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Presentation.Company.Selection;
using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Services.CompanySelection.Factions;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.Profiles;
using InfinityMercsApp.Services.CompanySelection.Recruitment;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.Services.CompanySelection.Teams;
using InfinityMercsApp.Services.CompanySelection.Units;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Season.Peripherals;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.ViewModels.Collections;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using InfinityMercsApp.ViewModels.Components.UnitFilter;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;
using TagGen = InfinityMercsApp.Infrastructure.Providers.CompanyFactions.TagCompanyFactionGenerator;

namespace InfinityMercsApp.ViewModels.Company.Selection;

/// <summary>
/// Building a new company: pick the faction(s) the units come from, browse their units or fireteams, look at
/// a unit's profiles and add them to the company, then name it and start it. What differs between kinds of
/// company (which factions, which units, extra rules when adding a trooper) is left to the subclasses.
/// </summary>
public abstract partial class CompanySelectionViewModel : AdaptiveViewModelBase<CompanySelectionLayout>
{
    private UnitFilterPopupOptions? _preparedFilterOptions;
    private ArmyFactionSelectionItem? _pickedFaction;
    private bool _restrictProfilesToFto;
    private bool _hasAppeared;
    private CompanyRecruitment? _recruitment;
    private bool _isPreparingRecruitment;

    [ObservableProperty]
    public partial bool ShowFactionStrip { get; set; } = true;

    /// <summary>True while the faction list is being loaded.</summary>
    [ObservableProperty]
    public partial bool IsLoadingFactions { get; set; }

    /// <summary>True while the chosen factions' units are being loaded.</summary>
    [ObservableProperty]
    public partial bool IsLoadingUnits { get; set; }

    [ObservableProperty]
    public partial ArmyFactionSelectionItem? LeftSlotFaction { get; set; }

    [ObservableProperty]
    public partial ArmyFactionSelectionItem? RightSlotFaction { get; set; }

    [ObservableProperty]
    public partial string? LeftSlotLogoSource { get; set; }

    [ObservableProperty]
    public partial string? RightSlotLogoSource { get; set; }

    [ObservableProperty]
    public partial int ActiveSlotIndex { get; set; }

    [ObservableProperty]
    public partial ArmyUnitSelectionItem? SelectedUnit { get; set; }

    [ObservableProperty]
    public partial bool LieutenantOnlyUnits { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowUnitsList), nameof(ShowTeamsList))]
    public partial bool TeamsView { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowUnitsList), nameof(ShowTeamsList))]
    public partial bool AreTeamEntriesReady { get; set; } = true;

    [ObservableProperty]
    public partial string ProfilesStatus { get; set; } = "Select a unit.";

    [ObservableProperty]
    public partial UnitFilterCriteria ActiveUnitFilter { get; set; } = UnitFilterCriteria.None;

    [ObservableProperty]
    public partial bool IsUnitFilterActive { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUnitFilterOpen))]
    public partial UnitFilterViewModel? UnitFilter { get; set; }

    [ObservableProperty]
    public partial string SelectedStartSeasonPoints { get; set; } = "75";

    /// <summary>The starting points picked in the title bar; they set the points limit while founding a company.</summary>
    [ObservableProperty]
    public partial string StartPointsChoice { get; set; } = "75";

    /// <summary>"40 CR LEFT" while recruiting.</summary>
    [ObservableProperty]
    public partial string CreditsLeftText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SeasonPointsCapText { get; set; } = "0";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCompanyCommand))]
    public partial bool IsCompanyValid { get; set; }

    [ObservableProperty]
    public partial string CompanyName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ShowCompanyNameValidationError { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUnitsPaneActive), nameof(IsProfilePaneActive), nameof(IsCompanyPaneActive))]
    public partial CompanySelectionPane ActivePane { get; set; } = CompanySelectionPane.Units;

    /// <summary>On compact screens the faction picker is a full-screen overlay while the strip is open.</summary>
    [ObservableProperty]
    public partial bool IsFactionOverlayOpen { get; set; }

    [ObservableProperty]
    public partial bool ShowsUnitsPane { get; set; }

    [ObservableProperty]
    public partial bool ShowsRightPane { get; set; }

    [ObservableProperty]
    public partial bool ShowsDetailPane { get; set; }

    [ObservableProperty]
    public partial bool ShowsCompanyPane { get; set; }

    [ObservableProperty]
    public partial string RightPaneRows { get; set; } = string.Empty;

    /// <summary>
    /// True when an existing company is hiring new troopers mid-season, rather than a company being founded.
    /// Its factions are fixed, its troopers are already on the roster, and its CR is the budget.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFoundingCompany))]
    public partial bool IsRecruiting { get; set; }

    [ObservableProperty]
    public partial string PageTitle { get; set; }

    [ObservableProperty]
    public partial string ConfirmText { get; set; } = "START COMPANY";

    protected CompanySelectionViewModel(CompanySelectionServices services, ArmySourceSelectionMode mode)
        : base(services.Navigation, CompanySelectionLayout.For)
    {
        Services = services;
        Mode = mode;
        PageTitle = Heading;
        ShowPanes();
    }

    protected CompanySelectionServices Services { get; }

    public ArmySourceSelectionMode Mode { get; }

    /// <summary>The selected unit as the unit-detail display shows it.</summary>
    public CompanyUnitDetail Detail { get; } = new();

    public ObservableRangeCollection<ArmyFactionSelectionItem> Factions { get; } = [];
    public ObservableCollection<ArmyUnitSelectionItem> Units { get; } = [];
    public ObservableCollection<ArmyTeamListItem> TeamEntries { get; } = [];
    public ObservableCollection<ViewerProfileItem> Profiles { get; } = [];
    public ObservableCollection<MercsCompanyEntry> MercsCompanyEntries { get; } = [];

    /// <summary>The page title and the faction picker's heading.</summary>
    public abstract string Heading { get; }

    /// <summary>The company type written to the saved company file.</summary>
    public abstract string CompanyTypeLabel { get; }

    public bool IsFoundingCompany => !IsRecruiting;

    /// <summary>Whether the company draws units from a second faction slot.</summary>
    public virtual bool ShowRightSlot => false;

    /// <summary>The line above the fireteam list.</summary>
    public virtual string FireteamRuleText => "Unlimited Duo fireteams allowed per company";

    /// <summary>Whether the fireteam list offers a tracked fireteam and its level (Cohesive companies).</summary>
    public virtual bool ShowsFireteamTracking => false;

    /// <summary>Whether roster entries show irregular/regular order badges (Cohesive companies).</summary>
    public virtual bool ShowsOrderModifierBadges => false;

    /// <summary>The tracked fireteam's name, when the company tracks one.</summary>
    public virtual string TrackedFireteamNameDisplay => string.Empty;

    /// <summary>The bonus the tracked fireteam's level gives, when it gives one.</summary>
    public virtual string? TrackedFireteamLevelBonusText => null;

    public virtual bool HasTrackedFireteamLevelBonus => false;

    /// <summary>The badge for the tracked fireteam's level, when it has one.</summary>
    public virtual string? TrackedFireteamLevelIcon => null;

    public bool ShowUnitsList => !TeamsView || (WaitsForTeamEntries && !AreTeamEntriesReady);

    public bool ShowTeamsList => TeamsView && (!WaitsForTeamEntries || AreTeamEntriesReady);

    public bool IsUnitFilterOpen => UnitFilter is not null;

    public bool IsUnitsPaneActive => ActivePane == CompanySelectionPane.Units;
    public bool IsProfilePaneActive => ActivePane == CompanySelectionPane.Profile;
    public bool IsCompanyPaneActive => ActivePane == CompanySelectionPane.Company;

    /// <summary>Whether the fireteam list waits for its entries to be ready, so it never flashes empty.</summary>
    protected virtual bool WaitsForTeamEntries => false;

    /// <summary>A slot every faction pick goes to, for companies whose other slot is fixed.</summary>
    protected virtual int? FixedFactionSlot => null;

    /// <summary>Whether picking a faction switches the units pane to fireteams.</summary>
    protected virtual bool ShowsTeamsAfterFactionPick => false;

    /// <summary>Whether the filter offers "lieutenants only".</summary>
    protected virtual bool AllowsLieutenantFilter => true;

    /// <summary>Whether the filter offers the fireteam view.</summary>
    protected virtual bool AllowsTeamsFilter => true;

    /// <summary>Whether the right slot can be picked to receive a faction.</summary>
    protected virtual bool CanPickRightSlot => ShowRightSlot;

    /// <summary>The points limit less what the company costs so far.</summary>
    protected int PointsRemaining => Points(SelectedStartSeasonPoints) - Points(SeasonPointsCapText);

    private static int Points(string text) => int.TryParse(text, out var points) ? points : 0;

    protected override void OnLayoutApplied() => ShowPanes();

    partial void OnActivePaneChanged(CompanySelectionPane value) => ShowPanes();

    partial void OnShowFactionStripChanged(bool value) => ShowPanes();

    partial void OnSelectedUnitChanged(ArmyUnitSelectionItem? value) => ShowPanes();

    // Which panes show for the layout, the pane picked on compact screens, and whether the faction picker is open.
    private void ShowPanes()
    {
        IsFactionOverlayOpen = IsCompact && ShowFactionStrip;
        ShowsUnitsPane = Layout.ShowsUnits(ActivePane);
        ShowsRightPane = Layout.ShowsRightPane(ActivePane);
        ShowsDetailPane = Layout.ShowsDetail(ActivePane, SelectedUnit is not null);
        ShowsCompanyPane = Layout.ShowsCompany(ActivePane);
        RightPaneRows = Layout.RightPaneRows(ActivePane, SelectedUnit is not null);
    }

    /// <summary>
    /// Turns the screen into recruiting for an existing company: its troopers go on the roster (counted for AVA
    /// and the company's rules, but not removable), and the points limit is what they cost plus the CR it has.
    /// Call before the screen first appears.
    /// </summary>
    public void PrepareRecruitment(CompanyRecruitment recruitment)
    {
        _recruitment = recruitment;
        IsRecruiting = true;
        PageTitle = $"Recruit for {recruitment.Company.CompanyName}";
        ConfirmText = "HIRE RECRUITS";
        CompanyName = recruitment.Company.CompanyName;
        _isPreparingRecruitment = true;
        try
        {
            MercsCompanyEntries.Clear();
            OnPreparingRecruitment(recruitment.Company);
            var servants = new ServantPeripherals(Services.ArmyData).For(recruitment.Company);
            foreach (var entry in RosterEntries.From(recruitment.Company, recruitment.RetiredEntries, servants))
            {
                MercsCompanyEntries.Add(entry);
            }

            var onRoster = MercsCompanyEntries.Sum(entry => entry.CostValue);
            SelectedStartSeasonPoints = (onRoster + Math.Max(0, recruitment.Credits)).ToString(CultureInfo.InvariantCulture);
            SeasonPointsCapText = RosterCost();
        }
        finally
        {
            _isPreparingRecruitment = false;
        }
    }

    /// <summary>Loads the faction list the first time the screen appears; while recruiting, the company's own factions.</summary>
    public override async Task InitializeAsync()
    {
        if (_hasAppeared)
        {
            return;
        }

        _hasAppeared = true;
        Detail.ShowUnitsInInches = Services.Settings?.GetShowUnitsInInches() ?? false;
        OnFirstAppearing();
        await LoadFactionsAsync();
        if (_recruitment is { } recruitment)
        {
            await StartRecruitingAsync(recruitment);
        }
    }

    // Puts the company's saved factions in the empty slots (a fixed slot is already filled) and lists their units.
    private async Task StartRecruitingAsync(CompanyRecruitment recruitment)
    {
        foreach (var saved in recruitment.Company.SourceFactions)
        {
            var faction = Factions.FirstOrDefault(candidate => candidate.Id == saved.FactionId) ?? new ArmyFactionSelectionItem
            {
                Id = saved.FactionId,
                ParentId = saved.FactionId,
                Name = saved.FactionName,
                CachedLogoPath = CachedFactionLogoPath(saved.FactionId),
                PackagedLogoPath = PackagedFactionLogoPath(saved.FactionId)
            };

            if (LeftSlotFaction is null)
            {
                SetSlotFaction(0, faction);
            }
            else if (ShowRightSlot && RightSlotFaction is null)
            {
                SetSlotFaction(1, faction);
            }
        }

        PrepareRecruitingSlots();
        ShowFactionStrip = false;
        TeamsView = ShowsTeamsAfterFactionPick;
        await LoadUnitsAsync();
        OnRosterChanged();
    }

    /// <summary>Where recruiting starts once the slots are filled; by default the slot already picked.</summary>
    protected virtual void PrepareRecruitingSlots()
    {
    }

    /// <summary>Fills a fixed faction slot before the factions load.</summary>
    protected virtual void OnFirstAppearing()
    {
    }

    // ── Factions and slots ──────────────────────────────────────────────────

    protected async Task LoadFactionsAsync(CancellationToken cancellationToken = default)
    {
        IsLoadingFactions = true;
        try
        {
            var records = await LoadFactionRecordsAsync(cancellationToken);
            Factions.ReplaceRange(records.Select(record => ArmyFactionSelectionItem.From(record, Services.Logos)));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Loading the factions failed: {ex.Message}");
        }
        finally
        {
            IsLoadingFactions = false;
        }
    }

    /// <summary>The factions the picker offers.</summary>
    protected abstract Task<IReadOnlyList<FactionRecord>> LoadFactionRecordsAsync(CancellationToken cancellationToken);

    /// <summary>Every non-NA faction of the screen's mode (vanilla or sectorial), with their logos cached.</summary>
    protected async Task<IReadOnlyList<FactionRecord>> LoadModeFactionRecordsAsync(CancellationToken cancellationToken) =>
        await Services.FactionCatalog.LoadAsync(Mode, cancellationToken);

    [RelayCommand]
    private Task SelectFactionAsync(ArmyFactionSelectionItem? item)
    {
        if (item is null || IsRecruiting)
        {
            return Task.CompletedTask;
        }

        if (!ReferenceEquals(_pickedFaction, item))
        {
            if (_pickedFaction is not null)
            {
                _pickedFaction.IsSelected = false;
            }

            _pickedFaction = item;
            item.IsSelected = true;
        }

        return AssignFactionAsync(item);
    }

    /// <summary>
    /// Puts the picked faction in the slot being edited (or the fixed slot), moves on to an empty slot,
    /// clears the company when the faction changed, and loads the units.
    /// </summary>
    protected virtual async Task AssignFactionAsync(ArmyFactionSelectionItem item)
    {
        var targetSlot = FixedFactionSlot ?? ActiveSlotIndex;
        var targetWasEmpty = targetSlot == 1 ? RightSlotFaction is null : LeftSlotFaction is null;
        if (!TryAssignFaction(targetSlot, item, out var factionChanged))
        {
            return;
        }

        var autoSelectUnit = factionChanged && LeftSlotFaction is not null && (!ShowRightSlot || RightSlotFaction is not null);

        if (FixedFactionSlot is int fixedSlot)
        {
            SetActiveSlot(fixedSlot);
        }
        else if (targetWasEmpty)
        {
            SetActiveSlot(FactionSlots.AfterFilling(ShowRightSlot, LeftSlotFaction is not null, RightSlotFaction is not null, ActiveSlotIndex));
        }
        else
        {
            SetActiveSlot(targetSlot);
        }

        if (factionChanged)
        {
            ResetCompany();
        }

        TeamsView = ShowsTeamsAfterFactionPick;
        if (LeftSlotFaction is not null && (!ShowRightSlot || RightSlotFaction is not null))
        {
            ShowFactionStrip = false;
        }

        await LoadUnitsAsync();
        if (autoSelectUnit && SelectedUnit is null && Units.FirstOrDefault(unit => unit.IsVisible) is { } firstVisible)
        {
            await SelectUnitAsync(firstVisible);
        }
    }

    /// <summary>Puts <paramref name="item"/> in slot <paramref name="slot"/> unless the other slot already has it.</summary>
    protected bool TryAssignFaction(int slot, ArmyFactionSelectionItem item, out bool factionChanged)
    {
        if (FactionSlots.IsInOtherSlot(ShowRightSlot, slot, LeftSlotFaction?.Id, RightSlotFaction?.Id, item.Id))
        {
            factionChanged = false;
            return false;
        }

        if (slot == 0 || !ShowRightSlot)
        {
            factionChanged = LeftSlotFaction?.Id != item.Id;
            SetSlotFaction(0, item);
        }
        else
        {
            factionChanged = RightSlotFaction?.Id != item.Id;
            SetSlotFaction(1, item);
        }

        return true;
    }

    protected void SetSlotFaction(int slot, ArmyFactionSelectionItem faction)
    {
        var logo = UnitLogo.Source(faction.CachedLogoPath, faction.PackagedLogoPath);
        if (slot == 0)
        {
            LeftSlotFaction = faction;
            LeftSlotLogoSource = logo;
        }
        else
        {
            RightSlotFaction = faction;
            RightSlotLogoSource = logo;
        }
    }

    /// <summary>
    /// Opens the faction picker for the tapped slot; tapping the slot the open picker is already filling closes it.
    /// A recruiting company's factions are fixed, so its picker stays shut.
    /// </summary>
    [RelayCommand]
    private void SelectSlot(int slot)
    {
        if (slot != 0 && !CanPickRightSlot)
        {
            return;
        }

        var closing = ShowFactionStrip && ActiveSlotIndex == slot;
        SetActiveSlot(slot);
        ShowFactionStrip = !closing && IsFoundingCompany;
    }

    /// <summary>Makes <paramref name="slot"/> the one the next faction pick goes to.</summary>
    protected virtual void SetActiveSlot(int slot) =>
        ActiveSlotIndex = FixedFactionSlot ?? FactionSlots.Active(slot, ShowRightSlot);

    [RelayCommand]
    private void OpenFactionStrip() => ShowFactionStrip = IsFoundingCompany;

    [RelayCommand]
    private void CloseFactionStrip() => ShowFactionStrip = false;

    [RelayCommand]
    private void ShowPane(CompanySelectionPane pane) => ActivePane = pane;

    // ── Units and fireteams ─────────────────────────────────────────────────

    /// <summary>The factions whose units are listed.</summary>
    protected virtual IReadOnlyList<ArmyFactionSelectionItem> UnitSourceFactions() =>
        FactionSlots.SourceFactions(ShowRightSlot, LeftSlotFaction, RightSlotFaction);

    /// <summary>Lists the units (and their fireteams) of the source factions.</summary>
    protected async Task LoadUnitsAsync(CancellationToken cancellationToken = default)
    {
        _preparedFilterOptions = null;
        AreTeamEntriesReady = false;
        Units.Clear();
        TeamEntries.Clear();
        ClearSelectedUnit();
        OnUnitsReloading();

        var factions = UnitSourceFactions();
        if (factions.Count == 0)
        {
            return;
        }

        IsLoadingUnits = true;
        try
        {
            var read = new List<FactionUnits>();
            foreach (var faction in factions)
            {
                var resumes = ResumesFor(faction.Id, cancellationToken);
                read.Add(Services.UnitCatalog.Read(faction.Id, resumes, cancellationToken));
                await CacheUnitLogosAsync(faction.Id, resumes, cancellationToken);
            }

            var merged = SelectionUnitCatalog.Merge(read);
            var units = merged.Units.Select(CreateUnit).ToList();
            foreach (var unit in ArmyUnitSort.OrderByUnitTypeAndName(units, x => x.Type, x => x.Name))
            {
                Units.Add(unit);
            }

            if (await TryRecoverEmptyUnitListAsync(cancellationToken))
            {
                return;
            }

            BeforeTeamsBuilt(factions);
            var listedTeams = merged.Teams.Where(team => IncludeTeam(team) && TeamCount(team) > 0);
            foreach (var listing in TeamListings.Build(units, listedTeams, merged.Teams))
            {
                TeamEntries.Add(new ArmyTeamListItem
                {
                    Name = listing.Name,
                    TeamCountsText = listing.Team is { } team ? TeamCountText(team) : string.Empty,
                    IsWildcardBucket = listing.IsWildcardBucket,
                    IsExpanded = true,
                    AllowedProfiles = new ObservableCollection<ArmyTeamUnitLimitItem>(
                        listing.Slots.Select(slot => ArmyTeamUnitLimitItem.For(slot, DisplayedTeamMinimum(slot.Min))))
                });
            }

            ApplyUnitVisibilityFilters(cancellationToken);
            BuildFilterOptions(cancellationToken);
            AfterUnitsLoaded();
            AreTeamEntriesReady = true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Loading the units failed: {ex.Message}");
            OnUnitsLoadFailed();
            AreTeamEntriesReady = false;
        }
        finally
        {
            IsLoadingUnits = false;
        }
    }

    /// <summary>A faction's units: by default only those Mercs companies can field.</summary>
    protected virtual IReadOnlyList<ArmyResumeRecord> ResumesFor(int factionId, CancellationToken cancellationToken) =>
        Services.ArmyData.GetResumeByFactionMercsOnly(factionId, cancellationToken);

    protected virtual async Task CacheUnitLogosAsync(int factionId, IReadOnlyList<ArmyResumeRecord> units, CancellationToken cancellationToken)
    {
        if (Services.Logos is { } logos)
        {
            await logos.CacheUnitLogosFromRecordsAsync(factionId, units, cancellationToken);
        }
    }

    /// <summary>Whose logo a unit shows; by default its own.</summary>
    protected virtual UnitLogoIds LogoIdsFor(int factionId, int unitId, string? logoField) => new(factionId, unitId);

    /// <summary>Whether a unit stands out in the list (the TAG company's own TAG).</summary>
    protected virtual bool IsHighlightedUnit(int factionId, int unitId) => false;

    private ArmyUnitSelectionItem CreateUnit(SelectionUnitSource unit)
    {
        var logo = LogoIdsFor(unit.FactionId, unit.UnitId, unit.LogoField);
        return new ArmyUnitSelectionItem
        {
            Id = unit.UnitId,
            SourceFactionId = unit.FactionId,
            LogoSourceFactionId = logo.FactionId,
            LogoSourceUnitId = logo.UnitId,
            Slug = unit.Slug,
            Name = unit.Name,
            Type = unit.Type,
            IsCharacter = unit.IsCharacter,
            Subtitle = unit.Subtitle,
            IsSpecOps = unit.IsSpecOps,
            UseBlueHalfOpacityBackground = !unit.IsSpecOps && IsHighlightedUnit(unit.FactionId, unit.UnitId),
            CachedLogoPath = Services.Logos?.TryGetCachedUnitLogoPath(logo.FactionId, logo.UnitId),
            PackagedLogoPath = PackagedUnitLogoPath(logo)
        };
    }

    private string PackagedUnitLogoPath(UnitLogoIds logo) =>
        Services.Logos?.GetPackagedUnitLogoPath(logo.FactionId, logo.UnitId) ?? $"SVGCache/units/{logo.FactionId}-{logo.UnitId}.svg";

    /// <summary>Called when the units are about to reload.</summary>
    protected virtual void OnUnitsReloading()
    {
    }

    /// <summary>Lets a company reload from another slot when its list came back empty; true when it did.</summary>
    protected virtual Task<bool> TryRecoverEmptyUnitListAsync(CancellationToken cancellationToken) => Task.FromResult(false);

    /// <summary>Called with the source factions just before the fireteams are built.</summary>
    protected virtual void BeforeTeamsBuilt(IReadOnlyList<ArmyFactionSelectionItem> factions)
    {
    }

    /// <summary>Which fireteams are listed; by default all with a Duo count.</summary>
    protected virtual bool IncludeTeam(MergedFireteam team) => true;

    protected virtual int TeamCount(MergedFireteam team) => team.Duo;

    protected virtual string TeamCountText(MergedFireteam team) => $"D: {team.Duo}";

    /// <summary>A fireteam slot's minimum as shown.</summary>
    protected virtual string DisplayedTeamMinimum(string min) => min;

    protected virtual void AfterUnitsLoaded()
    {
    }

    protected virtual void OnUnitsLoadFailed()
    {
    }

    /// <summary>
    /// Shows the units that fit the filter, the remaining points and (when asked) lieutenants only, drops the
    /// selected unit if it no longer shows, and updates which fireteam entries show.
    /// </summary>
    protected void ApplyUnitVisibilityFilters(CancellationToken cancellationToken = default)
    {
        if (Units.Count == 0)
        {
            return;
        }

        try
        {
            var context = Services.Visibility.Lookups(UnitSourceFactions().Select(faction => faction.Id), cancellationToken);
            ApplyUnitVisibility(context, cancellationToken);
            AdjustUnitVisibility(context, cancellationToken);

            if (SelectedUnit is { IsVisible: false } hidden)
            {
                hidden.IsSelected = false;
                SelectedUnit = null;
                ResetUnitDetails();
            }
            else if (SelectedUnit is not null)
            {
                ApplyLieutenantVisualStates();
            }

            RefreshTeamVisibility();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Filtering the units failed: {ex.Message}");
        }
    }

    protected void ApplyUnitVisibility(CompanyUnitVisibilityLookupContext context, CancellationToken cancellationToken)
    {
        foreach (var unit in Units)
        {
            cancellationToken.ThrowIfCancellationRequested();
            unit.IsVisible = Services.Visibility.Shows(context, unit, ActiveUnitFilter, LieutenantOnlyUnits, PointsRemaining, cancellationToken);
        }
    }

    // A fireteam shows while any of its slots does; a slot shows while a unit it stands for does.
    private void RefreshTeamVisibility()
    {
        var visible = VisibleUnits.Of(Units);
        foreach (var team in TeamEntries)
        {
            foreach (var slot in team.AllowedProfiles)
            {
                slot.IsVisible = visible.Shows(slot, team.IsWildcardBucket);
            }

            team.IsVisible = team.AllowedProfiles.Any(slot => slot.IsVisible);
            team.IsExpanded = true;
        }
    }

    /// <summary>Company-specific visibility rules; by default an empty result shows every unit rather than none.</summary>
    protected virtual void AdjustUnitVisibility(CompanyUnitVisibilityLookupContext context, CancellationToken cancellationToken)
    {
        if (!Units.Any(unit => unit.IsVisible))
        {
            foreach (var unit in Units)
            {
                unit.IsVisible = true;
            }
        }
    }

    // ── The selected unit ───────────────────────────────────────────────────

    [RelayCommand]
    private Task SelectUnitFromListAsync(ArmyUnitSelectionItem? item) =>
        item is null ? Task.CompletedTask : SelectUnitAsync(item);

    [RelayCommand]
    private Task SelectTeamAllowedProfileAsync(ArmyTeamUnitLimitItem? teamItem)
    {
        if (teamItem is null)
        {
            return Task.CompletedTask;
        }

        var resolved = TeamSlots.UnitFor(Units, teamItem);
        return resolved is null ? Task.CompletedTask : SelectUnitAsync(resolved, RestrictsProfilesFor(teamItem));
    }

    /// <summary>Whether picking a unit from this fireteam slot shows only its fireteam-only profiles.</summary>
    protected virtual bool RestrictsProfilesFor(ArmyTeamUnitLimitItem teamItem) => false;

    /// <summary>Shows <paramref name="item"/>'s statline, icons and profiles; reloads them when the profile restriction changed.</summary>
    protected async Task SelectUnitAsync(ArmyUnitSelectionItem item, bool restrictProfilesToFto = false)
    {
        var restrictionChanged = _restrictProfilesToFto != restrictProfilesToFto;
        _restrictProfilesToFto = restrictProfilesToFto;
        if (ReferenceEquals(SelectedUnit, item))
        {
            if (restrictionChanged)
            {
                LoadSelectedUnitDetails();
            }

            return;
        }

        if (SelectedUnit is not null)
        {
            SelectedUnit.IsSelected = false;
        }

        item.IsSelected = true;
        SelectedUnit = item;
        LoadSelectedUnitDetails();
        Detail.LogoSource = await UnitLogoSourceAsync(item);
    }

    /// <summary>Whether only fireteam-only profiles are shown for the selected unit.</summary>
    protected bool RestrictsProfilesToFto => _restrictProfilesToFto;

    private void ClearSelectedUnit()
    {
        if (SelectedUnit is not null)
        {
            SelectedUnit.IsSelected = false;
        }

        SelectedUnit = null;
        ResetUnitDetails();
    }

    private void ResetUnitDetails()
    {
        Detail.Reset();
        Profiles.Clear();
        ProfilesStatus = "Select a unit.";
    }

    /// <summary>Loads the selected unit's statline, trait icons, shared equipment and skills, and profiles.</summary>
    protected void LoadSelectedUnitDetails(CancellationToken cancellationToken = default)
    {
        Detail.Reset(clearLogo: false, resetHeaderColors: false);
        Profiles.Clear();
        ProfilesStatus = "Select a unit.";
        if (SelectedUnit is not { } unit)
        {
            return;
        }

        try
        {
            Detail.UnitNameHeading = unit.Name;
            var details = Services.UnitDetails.Load(
                unit.SourceFactionId, unit.Id, unit.IsSpecOps, LieutenantOnlyUnits, Detail.ShowUnitsInInches, cancellationToken);
            ApplyUnitHeaderColors(unit.SourceFactionId, details.Unit);
            if (details.ProfileGroups.IsEmpty)
            {
                Console.Error.WriteLine($"No profiles found for unit {unit.Id} of faction {unit.SourceFactionId}.");
                return;
            }

            Detail.SetProfileData(details.ProfileGroups, details.Filters);
            ApplyDisplayUnitsPreference();
            Detail.ApplyFirstProfileStats(details.ProfileGroups);
            if (!details.TreatAsSpecOps &&
                Services.UnitDetails.HighestAva(unit.Id, AvaFactionIds(unit), cancellationToken) is { } ava)
            {
                Detail.ApplyAva(ava);
            }

            var order = details.OrderTraits;
            Detail.SetOrderTraits(order.HasRegular, order.HasIrregular, order.HasImpetuous, order.HasTacticalAwareness);
            var tech = details.TechTraits;
            Detail.SetTechTraits(tech.HasCube, tech.HasCube2, tech.HasHackable);
            Detail.SetCommonEquipmentAndSkills(details.CommonEquipment, details.CommonSkills, details.TreatAsSpecOps);
            Detail.EquipmentSummary = $"Equipment: {Listed(details.CommonEquipment)}";
            Detail.SpecialSkillsSummary = $"Special Skills: {Listed(details.CommonSkills)}";
            PopulateProfiles(details.ProfileGroups, details.Filters, details.TreatAsSpecOps);
            UpdatePeripheralFromVisibleProfiles();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Loading the unit's details failed: {ex}");
        }
    }

    // The chosen factions that can list the unit; the TAG Company's own units are numbered apart from the rest.
    private IEnumerable<int> AvaFactionIds(ArmyUnitSelectionItem unit) =>
        unit.SourceFactionId == TagGen.TagCompanyFactionId
            ? [unit.SourceFactionId]
            : FactionSlots.SourceFactions(ShowRightSlot, LeftSlotFaction, RightSlotFaction)
                .Select(faction => faction.Id)
                .Where(id => id != TagGen.TagCompanyFactionId)
                .Append(unit.SourceFactionId)
                .Distinct();

    private static string Listed(IReadOnlyList<string> names) => names.Count == 0 ? "-" : string.Join(", ", names);

    private void ApplyUnitHeaderColors(int sourceFactionId, ArmyUnitRecord? unit) =>
        Detail.ApplyHeaderColors(Services.ThemeFactions.NameFor(Mode, sourceFactionId, unit?.FactionIds));

    // Picks up a changed inches/centimetres preference and re-formats every distance shown.
    private void ApplyDisplayUnitsPreference()
    {
        var showInches = Services.Settings?.GetShowUnitsInInches() ?? false;
        if (Detail.ShowUnitsInInches == showInches)
        {
            return;
        }

        Detail.ShowUnitsInInches = showInches;
        Detail.RefreshMoves();
        foreach (var entry in MercsCompanyEntries)
        {
            entry.RefreshMoves(showInches);
        }
    }


    private void PopulateProfiles(UnitProfileGroups profileGroups, ArmyFilterCatalog filters, bool forceLieutenant)
    {
        Profiles.Clear();
        if (profileGroups.IsEmpty)
        {
            ProfilesStatus = "No profiles found for this unit.";
            return;
        }

        var configurations = ProfileConfigurations.Build(new ProfileConfigurationRequest
        {
            ProfileGroups = profileGroups,
            Filters = filters,
            ForceLieutenant = forceLieutenant,
            ShowUnitsInInches = Detail.ShowUnitsInInches,
            FireteamOnly = RestrictsProfilesToFto,
            TacticalAwarenessDiscount = TacticalAwarenessDiscount
        });

        foreach (var configuration in configurations)
        {
            Profiles.Add(ViewerProfileItem.From(configuration, Detail.ShowUnitsInInches, Detail.Icons.TacticalAwareness));
        }

        ApplyLieutenantVisualStates();
    }

    /// <summary>Points off a profile with Tactical Awareness; none by default.</summary>
    protected virtual int TacticalAwarenessDiscount => 0;

    /// <summary>
    /// Shows the profiles the points, filter and lieutenant rules allow, blocks those that can't be added,
    /// and updates the status line, peripheral stats and company validity.
    /// </summary>
    protected void ApplyLieutenantVisualStates()
    {
        var room = RosterRoom.For(
            MercsCompanyEntries, Points(SelectedStartSeasonPoints), Points(SeasonPointsCapText), Detail.Stats.Ava, SelectedUnit?.Id, SelectedUnit?.SourceFactionId);
        var visibleProfiles = 0;
        foreach (var profile in Profiles)
        {
            var availability = ProfileAvailability.Of(
                ProfileValues.ParseCost(profile.Cost), profile.IsLieutenant, room, ActiveUnitFilter, LieutenantOnlyUnits);
            profile.IsVisible = availability.IsVisible;
            profile.IsLieutenantBlocked = availability.IsBlocked;
            if (availability.IsVisible)
            {
                visibleProfiles++;
            }
        }

        visibleProfiles = AdjustLieutenantVisualStates(visibleProfiles);

        ProfilesStatus = visibleProfiles == 0 ? "No configurations found for this unit." : $"{visibleProfiles} configurations loaded.";
        UpdatePeripheralFromVisibleProfiles();
        UpdateSeasonValidation();
    }

    /// <summary>A company's extra profile blocking; returns how many profiles still show.</summary>
    protected virtual int AdjustLieutenantVisualStates(int visibleProfiles) => visibleProfiles;

    // Shows the peripheral of the first visible profile that has one.
    private void UpdatePeripheralFromVisibleProfiles()
    {
        Detail.ResetPeripheral();
        if (Profiles.FirstOrDefault(profile => profile.IsVisible && profile.HasPeripherals) is { } profile &&
            PeripheralStatsOf(profile) is { } stats)
        {
            Detail.ApplyPeripheral(stats);
        }
    }

    // The statline of the profile's first peripheral, when the selected unit has its profile.
    private PeripheralMercsCompanyStats? PeripheralStatsOf(ViewerProfileItem profile)
    {
        var peripheralName = PeripheralNames.FirstOf(profile.Peripherals);
        return !string.IsNullOrWhiteSpace(peripheralName) && Detail.ProfileGroups.FindPeripheralProfile(peripheralName) is { } peripheralProfile
            ? PeripheralMercsCompanyStats.From(peripheralName, peripheralProfile, Detail.Filters, Detail.ShowUnitsInInches)
            : null;
    }

    /// <summary>
    /// The first logo that exists: a downloaded unit or faction logo, else a packaged one, else the unit's packaged path.
    /// </summary>
    protected async Task<string?> UnitLogoSourceAsync(ArmyUnitSelectionItem item)
    {
        if (CachedLogoCandidates(item).FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path)) is { } cachedPath)
        {
            return cachedPath;
        }

        foreach (var candidate in PackagedLogoCandidates(item).OfType<string>().Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var normalized = candidate.Replace('\\', '/').TrimStart('/');
            if (await Services.PackageFiles.ExistsAsync(normalized))
            {
                return normalized;
            }
        }

        return item.PackagedLogoPath;
    }

    // The unit's own downloaded logo, then its logo under each slot's faction, then its faction's logo.
    private IEnumerable<string?> CachedLogoCandidates(ArmyUnitSelectionItem item)
    {
        yield return item.CachedLogoPath;
        if (Services.Logos is not { } logos)
        {
            yield break;
        }

        foreach (var factionId in LogoFactions(item))
        {
            yield return logos.GetCachedUnitLogoPath(factionId, item.Id);
        }

        yield return CachedFactionLogoPath(item.SourceFactionId);
    }

    // The packaged logos in the same order, by the logo service's paths or the packaged folders' own.
    private IEnumerable<string?> PackagedLogoCandidates(ArmyUnitSelectionItem item)
    {
        yield return item.PackagedLogoPath;
        var logos = Services.Logos;
        foreach (var factionId in LogoFactions(item))
        {
            yield return logos?.GetPackagedUnitLogoPath(factionId, item.Id) ?? $"SVGCache/units/{factionId}-{item.Id}.svg";
        }

        yield return logos is null ? $"SVGCache/factions/{item.SourceFactionId}.svg" : PackagedFactionLogoPath(item.SourceFactionId);
    }

    // The unit's own faction, then the left slot's, then the right slot's when the company has one.
    private IEnumerable<int> LogoFactions(ArmyUnitSelectionItem item)
    {
        yield return item.SourceFactionId;
        if (LeftSlotFaction is { } left)
        {
            yield return left.Id;
        }

        if (ShowRightSlot && RightSlotFaction is { } right)
        {
            yield return right.Id;
        }
    }

    /// <summary>A faction's downloaded logo, used when a unit has none of its own.</summary>
    protected virtual string? CachedFactionLogoPath(int factionId) => Services.Logos?.GetCachedLogoPath(factionId);

    /// <summary>A faction's packaged logo, used when a unit has none of its own.</summary>
    protected virtual string? PackagedFactionLogoPath(int factionId) => Services.Logos?.GetPackagedFactionLogoPath(factionId);

    // ── The company ─────────────────────────────────────────────────────────

    /// <summary>Adds a profile of the selected unit to the company, lieutenants first.</summary>
    [RelayCommand]
    private async Task AddProfileToMercsCompanyAsync(ViewerProfileItem? profile)
    {
        if (profile is null || SelectedUnit is not { } unit || !profile.IsVisible || profile.IsLieutenantBlocked)
        {
            return;
        }

        if (!CanAddProfile(unit))
        {
            if (CannotAddMessage(unit) is { } message)
            {
                await Services.Dialogs.ShowAlertAsync("Can't Add Trooper", message);
            }

            return;
        }

        var commonSkills = CommonSkillsFor(unit, profile, Detail.CommonSkills);
        TrooperForm? form = null;
        var forms = TransmutationForms.FormsOf(Detail.ProfileGroups, profile.GroupId);
        if (forms.Count > 1 && commonSkills.Concat(CompanyProfileText.SplitDisplayLine(profile.UniqueSkills)).Any(TransmutationForms.IsAuto))
        {
            if (await ChooseFormAsync(profile.Name, forms) is not { } chosen)
            {
                return;
            }

            form = TransmutationForms.Of(chosen, Detail.Filters, Detail.ShowUnitsInInches);
        }

        MercsCompanyEntry? servantOwner = null;
        if (PeripheralKinds.IsServant(commonSkills.Concat(CompanyProfileText.SplitDisplayLine(profile.UniqueSkills))))
        {
            if (await ChooseServantOwnerAsync(profile.Name) is not { } owner)
            {
                return;
            }

            servantOwner = owner;
        }

        var entry = NewTrooperEntry.From(unit, profile, Detail, commonSkills, PeripheralStatsOf(profile), form, servantOwner);
        OnEntryBuilt(entry);

        if (entry.IsLieutenant)
        {
            MercsCompanyEntries.Insert(0, entry);
        }
        else if (servantOwner is not null)
        {
            MercsCompanyEntries.Insert(MercsCompanyEntries.IndexOf(servantOwner) + 1, entry);
        }
        else
        {
            MercsCompanyEntries.Add(entry);
        }

        OnRosterChanged();
        await AfterEntryAddedAsync(unit, profile, entry);
    }

    // Transmutation (Auto) is banned: the trooper is recruited in the one form the player picks; null when they back out.
    private async Task<UnitProfile?> ChooseFormAsync(string profileName, IReadOnlyList<UnitProfile> forms)
    {
        var labels = forms.Select((form, index) => string.IsNullOrWhiteSpace(form.Name) ? $"Form {index + 1}" : form.Name).ToList();
        var choice = await Services.Dialogs.ChooseAsync($"Choose {profileName}'s form", "Cancel", labels);
        var index = choice is null ? -1 : labels.IndexOf(choice);
        return index >= 0 ? forms[index] : null;
    }

    // A Servant recruited on its own belongs to a Doctor or Engineer in the company: the only one, or the one the
    // player picks; null when there is none or they back out.
    private async Task<MercsCompanyEntry?> ChooseServantOwnerAsync(string servantName)
    {
        var owners = MercsCompanyEntries.Where(entry => entry.CanOwnServant).ToList();
        switch (owners.Count)
        {
            case 0:
                await Services.Dialogs.ShowAlertAsync(
                    "Needs a Doctor or Engineer",
                    $"{servantName} must belong to a Doctor or Engineer in the company. Add one first.");
                return null;
            case 1:
                return owners[0];
        }

        var labels = owners
            .Select((owner, index) => owners.Count(other => other.Name == owner.Name) > 1 ? $"{owner.Name} ({index + 1})" : owner.Name)
            .ToList();
        var choice = await Services.Dialogs.ChooseAsync($"Who does {servantName} belong to?", "Cancel", labels);
        var chosen = choice is null ? -1 : labels.IndexOf(choice);
        return chosen >= 0 ? owners[chosen] : null;
    }

    /// <summary>Whether the selected unit can join the company at all.</summary>
    protected virtual bool CanAddProfile(ArmyUnitSelectionItem unit) => true;

    /// <summary>Why <see cref="CanAddProfile"/> turned the unit away, to tell the player; null to say nothing.</summary>
    protected virtual string? CannotAddMessage(ArmyUnitSelectionItem unit) => null;

    /// <summary>The skills every new trooper of this kind of company carries.</summary>
    protected virtual IReadOnlyCollection<string> CommonSkillsFor(
        ArmyUnitSelectionItem unit, ViewerProfileItem profile, IReadOnlyList<string> commonSkills) => commonSkills.ToList();

    protected virtual void OnEntryBuilt(MercsCompanyEntry entry)
    {
    }

    protected virtual Task AfterEntryAddedAsync(ArmyUnitSelectionItem unit, ViewerProfileItem profile, MercsCompanyEntry entry) =>
        Task.CompletedTask;

    /// <summary>
    /// Takes a trooper out of the company, with any Servant that belongs to it; one already on a recruiting
    /// company's roster stays.
    /// </summary>
    [RelayCommand]
    private void RemoveMercsCompanyEntry(MercsCompanyEntry? entry)
    {
        if (entry is not { IsOnRoster: false } || !MercsCompanyEntries.Remove(entry))
        {
            return;
        }

        foreach (var servant in MercsCompanyEntries.Where(candidate => candidate.ServantOwner == entry).ToList())
        {
            MercsCompanyEntries.Remove(servant);
        }

        OnRosterChanged();
    }

    /// <summary>Swaps an entry for its finished version (a TAG after its Spec Ops are chosen).</summary>
    protected void ReplaceMercsCompanyEntry(MercsCompanyEntry original, MercsCompanyEntry replacement)
    {
        var index = MercsCompanyEntries.IndexOf(original);
        if (index >= 0)
        {
            MercsCompanyEntries[index] = replacement;
            OnRosterChanged();
        }
    }

    // What the company's troopers cost, as the points shown.
    private string RosterCost() => MercsCompanyEntries.Sum(entry => entry.CostValue).ToString(CultureInfo.InvariantCulture);

    // Recounts the points; that refreshes validity, profile blocking and which units still fit.
    private void OnRosterChanged()
    {
        SeasonPointsCapText = RosterCost();
        AfterRosterChanged();
        ApplyLieutenantVisualStates();
        ApplyUnitVisibilityFilters();
    }

    /// <summary>Called whenever the company's troopers change.</summary>
    protected virtual void AfterRosterChanged()
    {
    }

    /// <summary>Shows a company trooper's unit, loading the unit when it isn't in the current list.</summary>
    [RelayCommand]
    private async Task SelectMercsCompanyEntryAsync(MercsCompanyEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        try
        {
            var unit = Units.FirstOrDefault(candidate => candidate.Id == entry.SourceUnitId && candidate.SourceFactionId == entry.SourceFactionId);
            if (unit is null)
            {
                var record = Services.ArmyData.GetUnit(entry.SourceFactionId, entry.SourceUnitId);
                unit = new ArmyUnitSelectionItem
                {
                    Id = entry.SourceUnitId,
                    SourceFactionId = entry.SourceFactionId,
                    Name = string.IsNullOrWhiteSpace(record?.Name) ? entry.Name : record.Name,
                    CachedLogoPath = entry.CachedLogoPath,
                    PackagedLogoPath = entry.PackagedLogoPath,
                    IsVisible = false
                };
            }

            await SelectUnitAsync(unit);
            LoadSelectedUnitDetails();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Showing the company trooper failed: {ex.Message}");
        }
    }

    /// <summary>Empties the company, e.g. when a source faction changes; a recruiting company keeps its roster.</summary>
    protected void ResetCompany()
    {
        foreach (var entry in MercsCompanyEntries.Where(entry => !entry.IsOnRoster).ToList())
        {
            MercsCompanyEntries.Remove(entry);
        }

        SeasonPointsCapText = RosterCost();
        AfterCompanyReset();
    }

    protected virtual void AfterCompanyReset()
    {
    }

    partial void OnSeasonPointsCapTextChanged(string value)
    {
        UpdateSeasonValidation();
        ApplyLieutenantVisualStates();
        ApplyUnitVisibilityFilters();
    }

    partial void OnSelectedStartSeasonPointsChanged(string value)
    {
        UpdateSeasonValidation();
        if (!_isPreparingRecruitment)
        {
            _ = OnPointsLimitChangedAsync();
        }
    }

    partial void OnStartPointsChoiceChanged(string value)
    {
        if (IsFoundingCompany && !string.IsNullOrWhiteSpace(value))
        {
            SelectedStartSeasonPoints = value;
        }
    }

    /// <summary>A new points limit: by default re-filters the units and rebuilds the filter's options.</summary>
    protected virtual Task OnPointsLimitChangedAsync()
    {
        _preparedFilterOptions = null;
        ApplyUnitVisibilityFilters();
        BuildFilterOptions();
        return Task.CompletedTask;
    }

    /// <summary>
    /// The company can start once it has a lieutenant and fits the points limit; a recruiting company also
    /// needs at least one recruit, and its remaining CR is shown.
    /// </summary>
    protected void UpdateSeasonValidation()
    {
        IsCompanyValid = MercsCompanyEntries.Any(entry => entry.IsLieutenant) &&
                         Points(SeasonPointsCapText) <= Points(SelectedStartSeasonPoints) &&
                         (IsFoundingCompany || MercsCompanyEntries.Any(entry => !entry.IsOnRoster));
        CreditsLeftText = IsRecruiting ? $"{PointsRemaining} CR LEFT" : string.Empty;
    }

    partial void OnLieutenantOnlyUnitsChanged(bool value)
    {
        ApplyLieutenantVisualStates();
        ApplyUnitVisibilityFilters();
    }

    partial void OnCompanyNameChanged(string value)
    {
        if (ShowCompanyNameValidationError && !string.IsNullOrWhiteSpace(value))
        {
            SetCompanyNameError(false);
        }
    }

    private void SetCompanyNameError(bool showError)
    {
        ShowCompanyNameValidationError = showError;
    }

    /// <summary>
    /// Configures the captain, saves the company and opens it; a missing name is flagged instead. While
    /// recruiting, hires the recruits instead.
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsCompanyValid))]
    private async Task StartCompanyAsync()
    {
        if (_recruitment is { } recruitment)
        {
            await HireRecruitsAsync(recruitment);
            return;
        }

        if (string.IsNullOrWhiteSpace(CompanyName))
        {
            SetCompanyNameError(true);
            return;
        }

        SetCompanyNameError(false);
        try
        {
            var founding = await Services.Founder.FoundAsync(new CompanyFoundingRequest<ArmyFactionSelectionItem, MercsCompanyEntry>
            {
                CompanyName = CompanyName.Trim(),
                CompanyType = CompanyTypeLabel,
                MercsCompanyEntries = MercsCompanyEntries,
                ShowRightSelectionBox = SavesRightSlot,
                LeftSlotFaction = LeftSlotFaction,
                RightSlotFaction = SavesRightSlot ? RightSlotFaction : null,
                Factions = Factions,
                ShowUnitsInInches = Detail.ShowUnitsInInches,
                SelectedStartSeasonPoints = SelectedStartSeasonPoints,
                SeasonPointsCapText = SeasonPointsCapText,
                CoreFireteam = CoreFireteam
            });
            switch (founding.Outcome)
            {
                case FoundingOutcome.Founded:
                    await NavigationService.NavigateToAsync("//SeasonPage", NavigationParameters.ForCompany(founding.FilePath));
                    break;
                case FoundingOutcome.NoTroopers:
                    await Services.Dialogs.ShowAlertAsync("Save Failed", "Add at least one unit before starting a company.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Starting the company failed: {ex}");
            await Services.Dialogs.ShowAlertAsync("Save Failed", ex.Message);
        }
    }

    /// <summary>Whether the right slot's faction is saved as a source of the company.</summary>
    protected virtual bool SavesRightSlot => ShowRightSlot;

    /// <summary>A Cohesive company's core fireteam, saved with it; empty for any other company.</summary>
    protected virtual string CoreFireteam => string.Empty;

    /// <summary>Takes up what the saved company fixed when it was founded, before its roster is listed.</summary>
    protected virtual void OnPreparingRecruitment(SavedCompanyFile company)
    {
    }

    // After confirming, adds the recruits to the company, charges their CR in the season and goes back.
    private async Task HireRecruitsAsync(CompanyRecruitment recruitment)
    {
        var recruits = MercsCompanyEntries.Where(entry => !entry.IsOnRoster).ToList();
        var cost = recruits.Sum(entry => entry.CostValue);
        var who = recruits.Count == 1 ? recruits[0].Name : $"{recruits.Count} recruits";
        if (!await Services.Dialogs.ConfirmAsync("Hire Recruits", $"Hire {who} for {cost} CR?"))
        {
            return;
        }

        var outcome = await Services.Recruiter.HireAsync(recruitment, MercsCompanyEntries.ToList());
        switch (outcome)
        {
            case RecruitmentOutcome.Hired:
                await NavigationService.PopAsync();
                break;
            case RecruitmentOutcome.CompanyNotSaved:
                await Services.Dialogs.ShowAlertAsync("Recruits Not Hired", "The company file could not be saved.");
                break;
            case RecruitmentOutcome.SeasonNotSaved:
                await Services.Dialogs.ShowAlertAsync("Recruits Not Hired", "The season file could not be saved, so nobody was hired.");
                break;
        }
    }

    // ── Filter ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void OpenUnitFilter()
    {
        var options = UnitFilterOptionsCatalog.WithMaxPoints(_preparedFilterOptions, FilterMaxPoints);
        var request = new UnitFilterRequest(
            options,
            ActiveUnitFilter,
            AllowsLieutenantFilter && LieutenantOnlyUnits,
            TeamsView: AllowsTeamsFilter && TeamsView,
            TeamsViewEnabled: AllowsTeamsFilter,
            LieutenantOnlyUnitsEnabled: AllowsLieutenantFilter);
        UnitFilter = new UnitFilterViewModel(request, ApplyUnitFilterCommand, CloseUnitFilterCommand);
    }

    [RelayCommand]
    private void CloseUnitFilter() => UnitFilter = null;

    [RelayCommand]
    private void ApplyUnitFilter(UnitFilterCriteria? criteria)
    {
        UnitFilter = null;
        ActiveUnitFilter = NormalizeFilterCriteria(criteria ?? UnitFilterCriteria.None);
        LieutenantOnlyUnits = ActiveUnitFilter.LieutenantOnlyUnits;
        TeamsView = ActiveUnitFilter.TeamsView;
        IsUnitFilterActive = ActiveUnitFilter.IsActive;
        ApplyUnitVisibilityFilters();
    }

    /// <summary>Drops filter settings the company doesn't allow.</summary>
    protected virtual UnitFilterCriteria NormalizeFilterCriteria(UnitFilterCriteria criteria) => criteria with
    {
        LieutenantOnlyUnits = AllowsLieutenantFilter && criteria.LieutenantOnlyUnits,
        TeamsView = AllowsTeamsFilter && criteria.TeamsView
    };

    private int FilterMaxPoints => UnitFilterOptionsCatalog.MaxPointsFor(SelectedStartSeasonPoints);

    /// <summary>Prepares the filter's choices from the source factions' units.</summary>
    protected void BuildFilterOptions(CancellationToken cancellationToken = default) =>
        _preparedFilterOptions = Services.FilterOptions.For(
            UnitSourceFactions().Select(faction => faction.Id).ToList(), FilterMaxPoints, cancellationToken);

    /// <summary>Forgets the prepared filter options so the next filter rebuilds them.</summary>
    protected void ForgetFilterOptions() => _preparedFilterOptions = null;
}
