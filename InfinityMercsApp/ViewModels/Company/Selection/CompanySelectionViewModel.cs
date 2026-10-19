using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Presentation.Company.Selection;
using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Services.CompanySelection.Factions;
using InfinityMercsApp.Services.CompanySelection.Profiles;
using InfinityMercsApp.Services.CompanySelection.Recruitment;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.Services.CompanySelection.Teams;
using InfinityMercsApp.Services.CompanySelection.Units;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Season.Peripherals;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using InfinityMercsApp.ViewModels.Company.Selection.Rules;
using InfinityMercsApp.ViewModels.Components.UnitFilter;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using TagGen = InfinityMercsApp.Infrastructure.Providers.CompanyFactions.TagCompanyFactionGenerator;

namespace InfinityMercsApp.ViewModels.Company.Selection;

/// <summary>
/// Building a new company, or recruiting for one: pick the faction(s) the units come from, browse their units or
/// fireteams, look at a unit's profiles and add them to the company, then name it and start it. The panes are
/// child ViewModels (<see cref="Slots"/>, <see cref="UnitList"/>, <see cref="Preview"/>, <see cref="Roster"/>);
/// this screen ties them together, and <see cref="Rules"/> says what the kind of company changes.
/// </summary>
public sealed partial class CompanySelectionViewModel : AdaptiveViewModelBase<CompanySelectionLayout>
{
    private readonly UnitLogoSources _logos;
    private bool _hasAppeared;
    private CompanyRecruitment? _recruitment;
    private bool _isPreparingRecruitment;

    [ObservableProperty]
    public partial bool ShowFactionStrip { get; set; } = true;

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

    public CompanySelectionViewModel(CompanySelectionServices services, CompanyRules rules)
        : base(services.Navigation, CompanySelectionLayout.For)
    {
        Services = services;
        Rules = rules;
        _logos = new UnitLogoSources(services.Logos, services.PackageFiles, rules.GeneratedFaction);
        PageTitle = rules.Heading;
        Slots = new FactionSlotsViewModel(rules.HasRightSlot);
        UnitList = new UnitListViewModel(waitsForTeamEntries: rules.TracksCoreFireteam);
        Preview = new UnitPreviewViewModel(services.UnitDetails, services.ThemeFactions, rules.Mode);
        Roster = new CompanyRosterViewModel(services.Dialogs);
        CoreFireteam = rules.TracksCoreFireteam ? new CoreFireteamTracker(UnitList.TeamEntries, Roster.Entries) : null;
        rules.Prepare(this);
        ShowPanes();
    }

    /// <summary>What the kind of company changes on this screen.</summary>
    public CompanyRules Rules { get; }

    public FactionSlotsViewModel Slots { get; }

    public UnitListViewModel UnitList { get; }

    public UnitPreviewViewModel Preview { get; }

    public CompanyRosterViewModel Roster { get; }

    /// <summary>A Cohesive company's tracked Core fireteam; null for any other company.</summary>
    public CoreFireteamTracker? CoreFireteam { get; }

    public ArmySourceSelectionMode Mode => Rules.Mode;

    /// <summary>The page title and the faction picker's heading.</summary>
    public string Heading => Rules.Heading;

    public string FireteamRuleText => Rules.FireteamRuleText;

    /// <summary>Whether the fireteam list offers a tracked fireteam, and roster entries show order badges.</summary>
    public bool TracksCoreFireteam => CoreFireteam is not null;

    public bool IsFoundingCompany => !IsRecruiting;

    public bool IsUnitsPaneActive => ActivePane == CompanySelectionPane.Units;
    public bool IsProfilePaneActive => ActivePane == CompanySelectionPane.Profile;
    public bool IsCompanyPaneActive => ActivePane == CompanySelectionPane.Company;

    internal CompanySelectionServices Services { get; }

    /// <summary>The points limit less what the company costs so far.</summary>
    private int PointsRemaining => Points(SelectedStartSeasonPoints) - Points(SeasonPointsCapText);

    private static int Points(string text) => int.TryParse(text, out var points) ? points : 0;

    protected override void OnLayoutApplied() => ShowPanes();

    partial void OnActivePaneChanged(CompanySelectionPane value) => ShowPanes();

    partial void OnShowFactionStripChanged(bool value) => ShowPanes();

    // Which panes show for the layout, the pane picked on compact screens, and whether the faction picker is open.
    private void ShowPanes()
    {
        var hasUnit = Preview.SelectedUnit is not null;
        IsFactionOverlayOpen = IsCompact && ShowFactionStrip;
        ShowsUnitsPane = Layout.ShowsUnits(ActivePane);
        ShowsRightPane = Layout.ShowsRightPane(ActivePane);
        ShowsDetailPane = Layout.ShowsDetail(ActivePane, hasUnit);
        ShowsCompanyPane = Layout.ShowsCompany(ActivePane);
        RightPaneRows = Layout.RightPaneRows(ActivePane, hasUnit);
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
        Roster.CompanyName = recruitment.Company.CompanyName;
        _isPreparingRecruitment = true;
        try
        {
            Roster.Entries.Clear();
            Rules.PrepareRecruitment(this, recruitment.Company);
            CoreFireteam?.Fix(recruitment.Company.CoreFireteam);
            var servants = new ServantPeripherals(Services.ArmyData).For(recruitment.Company);
            Roster.ShowCompany(RosterEntries.From(recruitment.Company, recruitment.RetiredEntries, servants));
            SelectedStartSeasonPoints = (Roster.Cost + Math.Max(0, recruitment.Credits)).ToString(CultureInfo.InvariantCulture);
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
        Preview.Detail.ShowUnitsInInches = Services.Settings?.GetShowUnitsInInches() ?? false;
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
            var faction = Slots.Factions.FirstOrDefault(candidate => candidate.Id == saved.FactionId) ?? new ArmyFactionSelectionItem
            {
                Id = saved.FactionId,
                ParentId = saved.FactionId,
                Name = saved.FactionName,
                CachedLogoPath = _logos.CachedFactionLogoPath(saved.FactionId),
                PackagedLogoPath = _logos.PackagedFactionLogoPath(saved.FactionId)
            };

            if (Slots.Left is null)
            {
                Slots.Set(0, faction);
            }
            else if (Slots.HasRightSlot && Slots.Right is null)
            {
                Slots.Set(1, faction);
            }
        }

        Rules.PrepareRecruitingSlots(this);
        ShowFactionStrip = false;
        UnitList.TeamsView = Rules.ShowsTeamsAfterFactionPick;
        await LoadUnitsAsync();
        OnRosterChanged();
    }

    // ── Factions and slots ──────────────────────────────────────────────────

    /// <summary>Lists the factions the company can pick from.</summary>
    internal async Task LoadFactionsAsync(CancellationToken cancellationToken = default)
    {
        Slots.IsLoading = true;
        try
        {
            var records = await Rules.LoadFactionsAsync(this, cancellationToken);
            Slots.Factions.ReplaceRange(records.Select(record => ArmyFactionSelectionItem.From(record, Services.Logos)));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Loading the factions failed: {ex.Message}");
        }
        finally
        {
            Slots.IsLoading = false;
        }
    }

    [RelayCommand]
    private Task SelectFactionAsync(ArmyFactionSelectionItem? item)
    {
        if (item is null || IsRecruiting)
        {
            return Task.CompletedTask;
        }

        Slots.Highlight(item);
        return Rules.AssignFactionAsync(this, item);
    }

    /// <summary>
    /// Puts the picked faction in the slot being edited (or the fixed slot), moves on to an empty slot,
    /// clears the company when the faction changed, and loads the units.
    /// </summary>
    internal async Task AssignToSlotAsync(ArmyFactionSelectionItem item)
    {
        var targetSlot = Rules.FixedFactionSlot ?? Slots.ActiveIndex;
        var targetWasEmpty = Slots.IsEmpty(targetSlot);
        if (!Slots.TryAssign(targetSlot, item, out var factionChanged))
        {
            return;
        }

        var autoSelectUnit = factionChanged && Slots.AreFilled;
        if (Rules.FixedFactionSlot is int fixedSlot)
        {
            Rules.SetActiveSlot(this, fixedSlot);
        }
        else if (targetWasEmpty)
        {
            Rules.SetActiveSlot(this, FactionSlots.AfterFilling(Slots.HasRightSlot, Slots.Left is not null, Slots.Right is not null, Slots.ActiveIndex));
        }
        else
        {
            Rules.SetActiveSlot(this, targetSlot);
        }

        if (factionChanged)
        {
            ResetCompany();
        }

        UnitList.TeamsView = Rules.ShowsTeamsAfterFactionPick;
        if (Slots.AreFilled)
        {
            ShowFactionStrip = false;
        }

        await LoadUnitsAsync();
        if (autoSelectUnit && Preview.SelectedUnit is null && UnitList.FirstVisible is { } firstVisible)
        {
            await SelectUnitAsync(firstVisible);
        }
    }

    /// <summary>
    /// Opens the faction picker for the tapped slot; tapping the slot the open picker is already filling closes it.
    /// A recruiting company's factions are fixed, so its picker stays shut.
    /// </summary>
    [RelayCommand]
    private void SelectSlot(int slot)
    {
        if (slot != 0 && !Rules.CanPickRightSlot)
        {
            return;
        }

        var closing = ShowFactionStrip && Slots.ActiveIndex == slot;
        Rules.SetActiveSlot(this, slot);
        ShowFactionStrip = !closing && IsFoundingCompany;
    }

    [RelayCommand]
    private void OpenFactionStrip() => ShowFactionStrip = IsFoundingCompany;

    [RelayCommand]
    private void CloseFactionStrip() => ShowFactionStrip = false;

    [RelayCommand]
    private void ShowPane(CompanySelectionPane pane) => ActivePane = pane;

    // ── Units and fireteams ─────────────────────────────────────────────────

    /// <summary>Lists the units (and their fireteams) of the source factions.</summary>
    internal async Task LoadUnitsAsync(CancellationToken cancellationToken = default)
    {
        UnitList.Clear();
        ClearSelectedUnit();
        CoreFireteam?.RememberForReload();

        var factions = Rules.UnitSources(this);
        if (factions.Count == 0)
        {
            return;
        }

        UnitList.IsLoading = true;
        try
        {
            var read = new List<FactionUnits>();
            foreach (var faction in factions)
            {
                var resumes = Rules.ResumesFor(Services.ArmyData, faction.Id, cancellationToken);
                read.Add(Services.UnitCatalog.Read(faction.Id, resumes, cancellationToken));
                await Rules.CacheUnitLogosAsync(Services.Logos, faction.Id, resumes, cancellationToken);
            }

            var merged = SelectionUnitCatalog.Merge(read);
            var units = merged.Units.Select(CreateUnit).ToList();
            foreach (var unit in ArmyUnitSort.OrderByUnitTypeAndName(units, x => x.Type, x => x.Name))
            {
                UnitList.Units.Add(unit);
            }

            if (await Rules.TryRecoverEmptyUnitListAsync(this, cancellationToken))
            {
                return;
            }

            CoreFireteam?.UseFactions(factions.Select(faction => faction.Id));
            var listedTeams = merged.Teams.Where(team => (CoreFireteam?.Lists(team) ?? true) && Rules.TeamCount(team) > 0);
            foreach (var listing in TeamListings.Build(units, listedTeams, merged.Teams))
            {
                UnitList.TeamEntries.Add(new ArmyTeamListItem
                {
                    Name = listing.Name,
                    TeamCountsText = listing.Team is { } team ? Rules.TeamCountText(team) : string.Empty,
                    IsWildcardBucket = listing.IsWildcardBucket,
                    IsExpanded = true,
                    AllowedProfiles = new ObservableCollection<ArmyTeamUnitLimitItem>(
                        listing.Slots.Select(slot => ArmyTeamUnitLimitItem.For(slot, Rules.DisplayedTeamMinimum(slot.Min))))
                });
            }

            ApplyUnitVisibilityFilters(cancellationToken);
            BuildFilterOptions(cancellationToken);
            CoreFireteam?.RestoreAfterReload();
            UnitList.AreTeamEntriesReady = true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Loading the units failed: {ex.Message}");
            CoreFireteam?.RestoreDefault();
            UnitList.AreTeamEntriesReady = false;
        }
        finally
        {
            UnitList.IsLoading = false;
        }
    }

    private ArmyUnitSelectionItem CreateUnit(SelectionUnitSource unit)
    {
        var logo = Rules.LogoIdsFor(unit.FactionId, unit.UnitId, unit.LogoField);
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
            UseBlueHalfOpacityBackground = !unit.IsSpecOps && Rules.IsHighlightedUnit(unit.FactionId, unit.UnitId),
            CachedLogoPath = Services.Logos?.TryGetCachedUnitLogoPath(logo.FactionId, logo.UnitId),
            PackagedLogoPath = _logos.PackagedUnitLogoPath(logo.FactionId, logo.UnitId)
        };
    }

    /// <summary>
    /// Shows the units that fit the filter, the remaining points and (when asked) lieutenants only, drops the
    /// selected unit if it no longer shows, and updates which fireteam entries show.
    /// </summary>
    internal void ApplyUnitVisibilityFilters(CancellationToken cancellationToken = default)
    {
        if (UnitList.Units.Count == 0)
        {
            return;
        }

        try
        {
            var context = Services.Visibility.Lookups(Rules.UnitSources(this).Select(faction => faction.Id), cancellationToken);
            ApplyUnitVisibility(context, cancellationToken);
            Rules.AdjustUnitVisibility(this, context, cancellationToken);

            if (Preview.SelectedUnit is { IsVisible: false })
            {
                ClearSelectedUnit();
            }
            else if (Preview.SelectedUnit is not null)
            {
                ApplyProfileAvailability();
            }

            UnitList.RefreshTeamVisibility();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Filtering the units failed: {ex.Message}");
        }
    }

    /// <summary>Shows each unit the filter, the points left and "lieutenants only" allow.</summary>
    internal void ApplyUnitVisibility(CompanyUnitVisibilityLookupContext context, CancellationToken cancellationToken)
    {
        foreach (var unit in UnitList.Units)
        {
            cancellationToken.ThrowIfCancellationRequested();
            unit.IsVisible = Services.Visibility.Shows(
                context, unit, UnitList.ActiveFilter, UnitList.LieutenantOnlyUnits, PointsRemaining, cancellationToken);
        }
    }

    /// <summary>A new points limit re-filters the units and rebuilds the filter's options.</summary>
    internal void RefreshUnitsForPoints()
    {
        UnitList.PreparedOptions = null;
        ApplyUnitVisibilityFilters();
        BuildFilterOptions();
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

        var resolved = TeamSlots.UnitFor(UnitList.Units, teamItem);
        return resolved is null ? Task.CompletedTask : SelectUnitAsync(resolved, Rules.RestrictsProfilesFor(teamItem));
    }

    /// <summary>Shows <paramref name="item"/>'s statline, icons and profiles; reloads them when the profile restriction changed.</summary>
    private async Task SelectUnitAsync(ArmyUnitSelectionItem item, bool restrictProfilesToFto = false)
    {
        var isAnotherUnit = !ReferenceEquals(Preview.SelectedUnit, item);
        if (!Preview.Select(item, restrictProfilesToFto))
        {
            return;
        }

        ShowPanes();
        LoadSelectedUnitDetails();
        if (isAnotherUnit)
        {
            Preview.Detail.LogoSource = await _logos.SourceAsync(
                new UnitLogoRequest(item.Id, item.SourceFactionId, item.CachedLogoPath, item.PackagedLogoPath, LogoFactionIds(item)));
        }
    }

    private void ClearSelectedUnit()
    {
        Preview.Clear();
        ShowPanes();
    }

    /// <summary>Loads the selected unit's details and profiles, and shows which profiles can be added.</summary>
    private void LoadSelectedUnitDetails()
    {
        try
        {
            ApplyDisplayUnitsPreference();
            if (Preview.SelectedUnit is { } unit &&
                Preview.Load(new UnitPreviewRequest(UnitList.LieutenantOnlyUnits, AvaFactionIds(unit), Rules.TacticalAwarenessDiscount)))
            {
                ApplyProfileAvailability();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Loading the unit's details failed: {ex}");
        }
    }

    // The chosen factions that can list the unit; the TAG Company's own units are numbered apart from the rest.
    private IReadOnlyList<int> AvaFactionIds(ArmyUnitSelectionItem unit) =>
        unit.SourceFactionId == TagGen.TagCompanyFactionId
            ? [unit.SourceFactionId]
            : Slots.Sources
                .Select(faction => faction.Id)
                .Where(id => id != TagGen.TagCompanyFactionId)
                .Append(unit.SourceFactionId)
                .Distinct()
                .ToList();

    // The unit's own faction, then the left slot's, then the right slot's when the company has one.
    private IReadOnlyList<int> LogoFactionIds(ArmyUnitSelectionItem item)
    {
        var ids = new List<int> { item.SourceFactionId };
        if (Slots.Left is { } left)
        {
            ids.Add(left.Id);
        }

        if (Slots.HasRightSlot && Slots.Right is { } right)
        {
            ids.Add(right.Id);
        }

        return ids;
    }

    // Picks up a changed inches/centimetres preference and re-formats every distance shown.
    private void ApplyDisplayUnitsPreference()
    {
        var showInches = Services.Settings?.GetShowUnitsInInches() ?? false;
        if (Preview.Detail.ShowUnitsInInches == showInches)
        {
            return;
        }

        Preview.Detail.ShowUnitsInInches = showInches;
        Preview.Detail.RefreshMoves();
        Roster.RefreshMoves(showInches);
    }

    /// <summary>
    /// Shows the profiles the points, filter and lieutenant rules allow, blocks those that can't be added,
    /// and updates the status line, peripheral stats and company validity.
    /// </summary>
    private void ApplyProfileAvailability()
    {
        var unit = Preview.SelectedUnit;
        var room = RosterRoom.For(
            Roster.Entries, Points(SelectedStartSeasonPoints), Points(SeasonPointsCapText), Preview.Detail.Stats.Ava,
            unit?.LogoSourceUnitId is > 0 ? unit.LogoSourceUnitId : unit?.Id,
            unit?.LogoSourceFactionId is > 0 ? unit.LogoSourceFactionId : unit?.SourceFactionId);
        var visibleProfiles = Preview.ApplyAvailability(room, UnitList.ActiveFilter, UnitList.LieutenantOnlyUnits);
        Preview.ShowAvailability(Rules.AdjustProfileAvailability(this, visibleProfiles));
        UpdateSeasonValidation();
    }

    // ── The company ─────────────────────────────────────────────────────────

    /// <summary>Adds a profile of the selected unit to the company, lieutenants first.</summary>
    [RelayCommand]
    private async Task AddProfileToMercsCompanyAsync(ViewerProfileItem? profile)
    {
        if (profile is null || Preview.SelectedUnit is not { } unit || !profile.IsVisible || profile.IsLieutenantBlocked)
        {
            return;
        }

        if (!Rules.CanAddProfile(this, unit))
        {
            if (Rules.CannotAddMessage(this, unit) is { } message)
            {
                await Services.Dialogs.ShowAlertAsync("Can't Add Trooper", message);
            }

            return;
        }

        var detail = Preview.Detail;
        var commonSkills = Rules.CommonSkillsFor(unit, profile, detail.CommonSkills);
        var skills = commonSkills.Concat(CompanyProfileText.SplitDisplayLine(profile.UniqueSkills)).ToList();
        TrooperForm? form = null;
        var forms = TransmutationForms.FormsOf(detail.ProfileGroups, profile.GroupId);
        if (forms.Count > 1 && skills.Any(TransmutationForms.IsAuto))
        {
            if (await ChooseFormAsync(profile.Name, forms) is not { } chosen)
            {
                return;
            }

            form = TransmutationForms.Of(chosen, detail.Filters, detail.ShowUnitsInInches);
        }

        MercsCompanyEntry? servantOwner = null;
        if (PeripheralKinds.IsServant(skills))
        {
            if (await Roster.ChooseServantOwnerAsync(profile.Name) is not { } owner)
            {
                return;
            }

            servantOwner = owner;
        }

        var entry = NewTrooperEntry.From(unit, profile, detail, commonSkills, Preview.PeripheralStatsOf(profile), form, servantOwner);
        Rules.OnEntryBuilt(this, entry);
        Roster.Add(entry, servantOwner);
        OnRosterChanged();
        await Rules.AfterEntryAddedAsync(this, unit, profile, entry);
    }

    // Transmutation (Auto) is banned: the trooper is recruited in the one form the player picks; null when they back out.
    private async Task<UnitProfile?> ChooseFormAsync(string profileName, IReadOnlyList<UnitProfile> forms)
    {
        var labels = forms.Select((form, index) => string.IsNullOrWhiteSpace(form.Name) ? $"Form {index + 1}" : form.Name).ToList();
        var choice = await Services.Dialogs.ChooseAsync($"Choose {profileName}'s form", "Cancel", labels);
        var index = choice is null ? -1 : labels.IndexOf(choice);
        return index >= 0 ? forms[index] : null;
    }

    /// <summary>
    /// Takes a trooper out of the company, with any Servant that belongs to it; one already on a recruiting
    /// company's roster stays.
    /// </summary>
    [RelayCommand]
    private void RemoveMercsCompanyEntry(MercsCompanyEntry? entry)
    {
        if (entry is not null && Roster.Remove(entry))
        {
            OnRosterChanged();
        }
    }

    /// <summary>Swaps an entry for its finished version (a TAG after its Spec Ops are chosen).</summary>
    internal void ReplaceEntry(MercsCompanyEntry original, MercsCompanyEntry replacement)
    {
        if (Roster.Replace(original, replacement))
        {
            OnRosterChanged();
        }
    }

    // What the company's troopers cost, as the points shown.
    private string RosterCost() => Roster.Cost.ToString(CultureInfo.InvariantCulture);

    // Recounts the points; that refreshes validity, profile blocking and which units still fit.
    private void OnRosterChanged()
    {
        SeasonPointsCapText = RosterCost();
        CoreFireteam?.Reevaluate();
        ApplyProfileAvailability();
        ApplyUnitVisibilityFilters();
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
            var unit = UnitList.Units.FirstOrDefault(candidate => candidate.Id == entry.SourceUnitId && candidate.SourceFactionId == entry.SourceFactionId);
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
    internal void ResetCompany()
    {
        Roster.RemoveRecruits();
        SeasonPointsCapText = RosterCost();
        CoreFireteam?.Reevaluate();
    }

    partial void OnSeasonPointsCapTextChanged(string value)
    {
        UpdateSeasonValidation();
        ApplyProfileAvailability();
        ApplyUnitVisibilityFilters();
    }

    partial void OnSelectedStartSeasonPointsChanged(string value)
    {
        UpdateSeasonValidation();
        if (!_isPreparingRecruitment)
        {
            _ = Rules.OnPointsLimitChangedAsync(this);
        }
    }

    partial void OnStartPointsChoiceChanged(string value)
    {
        if (IsFoundingCompany && !string.IsNullOrWhiteSpace(value))
        {
            SelectedStartSeasonPoints = value;
        }
    }

    /// <summary>
    /// The company can start once it has a lieutenant and fits the points limit; a recruiting company also
    /// needs at least one recruit, and its remaining CR is shown.
    /// </summary>
    private void UpdateSeasonValidation()
    {
        IsCompanyValid = Roster.HasLieutenant &&
                         Points(SeasonPointsCapText) <= Points(SelectedStartSeasonPoints) &&
                         (IsFoundingCompany || Roster.Recruits.Count > 0);
        CreditsLeftText = IsRecruiting ? $"{PointsRemaining} CR LEFT" : string.Empty;
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

        if (string.IsNullOrWhiteSpace(Roster.CompanyName))
        {
            Roster.ShowCompanyNameValidationError = true;
            return;
        }

        Roster.ShowCompanyNameValidationError = false;
        try
        {
            var founding = await Services.Founder.FoundAsync(new CompanyFoundingRequest<ArmyFactionSelectionItem, MercsCompanyEntry>
            {
                CompanyName = Roster.CompanyName.Trim(),
                CompanyType = Rules.CompanyTypeLabel,
                MercsCompanyEntries = Roster.Entries,
                ShowRightSelectionBox = Rules.SavesRightSlot,
                LeftSlotFaction = Slots.Left,
                RightSlotFaction = Rules.SavesRightSlot ? Slots.Right : null,
                Factions = Slots.Factions,
                ShowUnitsInInches = Preview.Detail.ShowUnitsInInches,
                SelectedStartSeasonPoints = SelectedStartSeasonPoints,
                SeasonPointsCapText = SeasonPointsCapText,
                CoreFireteam = CoreFireteam?.Name ?? string.Empty,
                CoreFireteamSlots = CoreFireteam?.Slots() ?? []
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

    // After confirming, adds the recruits to the company, charges their CR in the season and goes back.
    private async Task HireRecruitsAsync(CompanyRecruitment recruitment)
    {
        var recruits = Roster.Recruits;
        var cost = recruits.Sum(entry => entry.CostValue);
        var who = recruits.Count == 1 ? recruits[0].Name : $"{recruits.Count} recruits";
        if (!await Services.Dialogs.ConfirmAsync("Hire Recruits", $"Hire {who} for {cost} CR?"))
        {
            return;
        }

        var outcome = await Services.Recruiter.HireAsync(recruitment, recruits);
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
        var options = UnitFilterOptionsCatalog.WithMaxPoints(UnitList.PreparedOptions, FilterMaxPoints);
        var request = new UnitFilterRequest(
            options,
            UnitList.ActiveFilter,
            Rules.AllowsLieutenantFilter && UnitList.LieutenantOnlyUnits,
            TeamsView: Rules.AllowsTeamsFilter && UnitList.TeamsView,
            TeamsViewEnabled: Rules.AllowsTeamsFilter,
            LieutenantOnlyUnitsEnabled: Rules.AllowsLieutenantFilter);
        UnitList.Filter = new UnitFilterViewModel(request, ApplyUnitFilterCommand, CloseUnitFilterCommand);
    }

    [RelayCommand]
    private void CloseUnitFilter() => UnitList.Filter = null;

    /// <summary>Takes the popup's filter, less what the company doesn't allow, and re-filters the units and profiles.</summary>
    [RelayCommand]
    private void ApplyUnitFilter(UnitFilterCriteria? criteria)
    {
        var applied = criteria ?? UnitFilterCriteria.None;
        UnitList.Filter = null;
        UnitList.Apply(applied with
        {
            LieutenantOnlyUnits = Rules.AllowsLieutenantFilter && applied.LieutenantOnlyUnits,
            TeamsView = Rules.AllowsTeamsFilter && applied.TeamsView
        });
        ApplyProfileAvailability();
        ApplyUnitVisibilityFilters();
    }

    private int FilterMaxPoints => UnitFilterOptionsCatalog.MaxPointsFor(SelectedStartSeasonPoints);

    /// <summary>Prepares the filter's choices from the source factions' units.</summary>
    private void BuildFilterOptions(CancellationToken cancellationToken = default) =>
        UnitList.PreparedOptions = Services.FilterOptions.For(
            Rules.UnitSources(this).Select(faction => faction.Id).ToList(), FilterMaxPoints, cancellationToken);
}
