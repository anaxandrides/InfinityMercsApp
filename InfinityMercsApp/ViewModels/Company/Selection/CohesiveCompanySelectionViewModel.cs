using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Services.CompanySelection.Models.Entries;
using InfinityMercsApp.Services.CompanySelection.Models.Selection;
using InfinityMercsApp.Services.CompanySelection.Models.Teams;
using InfinityMercsApp.Services.CompanySelection.Models.Ui;
using InfinityMercsApp.Services.CompanySelection.Teams;
using InfinityMercsApp.Services.CompanySelection.Units;
using InfinityMercsApp.Services.Navigation;
using CCFactionFireteamValidityRecord = InfinityMercsApp.Domain.Models.Army.CCFactionFireteamValidityRecord;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;

namespace InfinityMercsApp.ViewModels.Company.Selection;

/// <summary>
/// A Cohesive company: one sectorial that can field a Core fireteam within the points limit. The player
/// tracks one fireteam; its level gives a bonus, and troopers outside it are irregular. Troopers get
/// Number 2 (and the lieutenant FT Master), and Tactical Awareness costs 3 points less.
/// </summary>
public sealed partial class CohesiveCompanySelectionViewModel : CompanySelectionViewModel
{
    public const string NumberTwoSkill = "Number 2";
    public const string FireteamMasterSkill = "FT Master";
    public const int TacticalAwarenessDiscount = 3;

    private readonly Dictionary<int, HashSet<string>> _validCoreFireteamsByFaction = [];
    private HashSet<string> _validCoreTeamNames = new(StringComparer.OrdinalIgnoreCase);
    private string _trackedFireteamToRestore = string.Empty;
    private bool _isUpdatingTrackedTeam;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TrackedFireteamNameDisplay))]
    private string _trackedFireteamName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TrackedFireteamLevelBonusText), nameof(HasTrackedFireteamLevelBonus), nameof(TrackedFireteamLevelIcon))]
    private int _trackedFireteamLevel;

    public CohesiveCompanySelectionViewModel(CompanySelectionServices services, ArmySourceSelectionMode mode)
        : base(services, mode)
    {
        TeamsView = true;
        IsUnitFilterActive = true;
        TeamEntries.CollectionChanged += (_, e) =>
        {
            foreach (var team in e.NewItems?.OfType<ArmyTeamListItem>() ?? [])
            {
                team.PropertyChanged += OnTeamPropertyChanged;
            }
        };
    }

    public override string Heading => "Choose your sectorial:";

    public override string CompanyTypeLabel => "Cohesive Company";

    public override string FireteamRuleText => "One Core fireteam per company";

    public override bool ShowsFireteamTracking => true;

    public override bool ShowsOrderModifierBadges => true;

    public override string TrackedFireteamNameDisplay => string.IsNullOrWhiteSpace(TrackedFireteamName) ? "Select fireteam" : TrackedFireteamName;

    public override string? TrackedFireteamLevelBonusText => FireteamLevels.BonusText(TrackedFireteamLevel);

    public override bool HasTrackedFireteamLevelBonus => !string.IsNullOrEmpty(TrackedFireteamLevelBonusText);

    /// <summary>The badge for the tracked fireteam's level, or none below level 1.</summary>
    public override string? TrackedFireteamLevelIcon => TrackedFireteamLevel is >= 1 and <= FireteamLevels.MaxLevel
        ? $"SVGCache/NonCBIcons/Fireteam/noun-team-{TrackedFireteamLevel}.svg"
        : null;

    protected override bool WaitsForTeamEntries => true;

    protected override bool ShowsTeamsAfterFactionPick => true;

    // ── Factions ────────────────────────────────────────────────────────────

    /// <summary>The sectorials that can field a valid Core fireteam; each answer is cached per points limit and filter.</summary>
    protected override async Task<IReadOnlyList<FactionRecord>> LoadFactionRecordsAsync(CancellationToken cancellationToken)
    {
        var maxCost = int.TryParse(SelectedStartSeasonPoints, NumberStyles.Integer, CultureInfo.InvariantCulture, out var limit) ? limit : 0;
        var ordered = await LoadModeFactionRecordsAsync(cancellationToken);
        var filterKey = ValidityFilterKey(maxCost);
        var cachedByFaction = Services.SpecOps
            .GetCCFactionFireteamValidity(filterKey, ordered.Select(faction => faction.Id).ToList(), cancellationToken)
            .GroupBy(row => row.FactionId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(row => row.EvaluatedAtUnixSeconds).First());

        foreach (var faction in ordered.Where(faction => !cachedByFaction.ContainsKey(faction.Id)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var validTeams = EvaluateValidCoreFireteams(faction, maxCost, cancellationToken);
            Services.SpecOps.UpsertCCFactionFireteamValidity(faction.Id, filterKey, validTeams.Count > 0, validTeams, cancellationToken);
            cachedByFaction[faction.Id] = new CCFactionFireteamValidityRecord
            {
                FactionId = faction.Id,
                FilterKey = filterKey,
                HasValidCoreFireteams = validTeams.Count > 0,
                ValidCoreFireteams = validTeams,
                EvaluatedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        _validCoreFireteamsByFaction.Clear();
        foreach (var row in cachedByFaction.Values)
        {
            _validCoreFireteamsByFaction[row.FactionId] = CohesiveCompanyTeamEligibilityWorkflow.ToValidCoreFireteamSet(row.ValidCoreFireteams);
        }

        return ordered.Where(faction => cachedByFaction.TryGetValue(faction.Id, out var row) && row.HasValidCoreFireteams).ToList();
    }

    private List<string> EvaluateValidCoreFireteams(FactionRecord faction, int maxCost, CancellationToken cancellationToken)
    {
        var snapshot = Services.ArmyData.GetFactionSnapshot(faction.Id, cancellationToken);
        if (snapshot?.FireteamChart is null)
        {
            return [];
        }

        var typeLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot.Filters, ArmyFilterSection.Type);
        var categoryLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot.Filters, ArmyFilterSection.Category);
        return CohesiveCompanyTeamEligibilityWorkflow.EvaluateValidCoreFireteams(
            snapshot.FireteamChart,
            Services.ArmyData.GetResumeByFactionMercsOnly(faction.Id, cancellationToken),
            UnitFilterLookups.From(snapshot.Filters),
            typeLookup,
            ActiveUnitFilter,
            maxCost,
            unit => CompanyUnitDetailsShared.IsCharacterCategory(unit, categoryLookup),
            unitId => Services.ArmyData.GetUnit(faction.Id, unitId, cancellationToken));
    }

    // What the cached validity depends on: the points limit and the filter.
    private string ValidityFilterKey(int maxCost)
    {
        var terms = string.Join(";", ActiveUnitFilter.ToQuery().Terms
            .OrderBy(term => term.Field)
            .Select(term => $"{term.Field}:{term.MatchMode}:{string.Join(",", term.Values.Order(StringComparer.OrdinalIgnoreCase))}"));

        return string.Join("|",
            "cc-core-v2",
            $"pts:{maxCost}",
            $"lt:{(LieutenantOnlyUnits ? 1 : 0)}",
            $"terms:{terms}",
            $"min:{ActiveUnitFilter.MinPoints?.ToString(CultureInfo.InvariantCulture) ?? string.Empty}",
            $"max:{ActiveUnitFilter.MaxPoints?.ToString(CultureInfo.InvariantCulture) ?? string.Empty}",
            $"filterlt:{(ActiveUnitFilter.LieutenantOnlyUnits ? 1 : 0)}");
    }

    /// <summary>A new points limit changes which sectorials and fireteams are valid, so everything reloads.</summary>
    protected override async Task OnPointsLimitChangedAsync()
    {
        try
        {
            ForgetFilterOptions();
            await LoadFactionsAsync();
            await LoadUnitsAsync();
            ApplyUnitVisibilityFilters();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Reloading for the new points limit failed: {ex.Message}");
        }
    }

    // ── Fireteams ───────────────────────────────────────────────────────────

    protected override void OnUnitsReloading() => _trackedFireteamToRestore = TrackedFireteamName;

    protected override void BeforeTeamsBuilt(IReadOnlyList<ArmyFactionSelectionItem> factions) =>
        _validCoreTeamNames = factions
            .SelectMany(faction => _validCoreFireteamsByFaction.TryGetValue(faction.Id, out var names) ? names : [])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Only the Core fireteams the company can field; all of them when none were evaluated.</summary>
    protected override bool IncludeTeam(MergedFireteam team) => _validCoreTeamNames.Count == 0 || _validCoreTeamNames.Contains(team.Name);

    protected override int TeamCount(MergedFireteam team) => team.Core;

    protected override string TeamCountText(MergedFireteam team) => $"C: {team.Core}";

    /// <summary>A Cohesive company needn't fill a fireteam's minimums, so they all read 0.</summary>
    protected override string DisplayedTeamMinimum(string min) =>
        string.Equals(min?.Trim(), "*", StringComparison.Ordinal) ||
        (int.TryParse(min, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
            ? "0"
            : min ?? "0";

    protected override void AfterUnitsLoaded() => RestoreTrackedFireteam(_trackedFireteamToRestore);

    protected override void OnUnitsLoadFailed() => RestoreTrackedFireteam(string.Empty);

    private void OnTeamPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_isUpdatingTrackedTeam &&
            e.PropertyName == nameof(ArmyTeamListItem.IsTrackedTeam) &&
            sender is ArmyTeamListItem { IsTrackedTeam: true, ShowTrackingRadioButton: true } team)
        {
            SetTrackedFireteam(team.Name);
        }
    }

    private void RestoreTrackedFireteam(string? name)
    {
        var match = string.IsNullOrWhiteSpace(name)
            ? null
            : TeamEntries.FirstOrDefault(team => team.ShowTrackingRadioButton && string.Equals(team.Name, name, StringComparison.OrdinalIgnoreCase));
        SetTrackedFireteam(match?.Name ?? DefaultTrackedFireteam());
    }

    private string DefaultTrackedFireteam() =>
        (TeamEntries.FirstOrDefault(team => team.ShowTrackingRadioButton && team.IsVisible)
         ?? TeamEntries.FirstOrDefault(team => team.ShowTrackingRadioButton))?.Name
        ?? string.Empty;

    /// <summary>Tracks <paramref name="teamName"/> and re-evaluates its level and who is irregular.</summary>
    public void SetTrackedFireteam(string? teamName)
    {
        var normalized = teamName?.Trim() ?? string.Empty;
        _isUpdatingTrackedTeam = true;
        try
        {
            foreach (var team in TeamEntries)
            {
                team.IsTrackedTeam = team.ShowTrackingRadioButton &&
                                     normalized.Length > 0 &&
                                     string.Equals(team.Name, normalized, StringComparison.OrdinalIgnoreCase);
            }
        }
        finally
        {
            _isUpdatingTrackedTeam = false;
        }

        TrackedFireteamName = normalized;
        ReevaluateTrackedFireteam();
    }

    private void ReevaluateTrackedFireteam()
    {
        var slots = TrackedFireteamSlots();
        TrackedFireteamLevel = slots is null ? 0 : Math.Clamp(FireteamLevels.Level(Members(), slots), 0, FireteamLevels.MaxLevel);

        // Without a tracked fireteam nobody is irregular.
        var claimed = slots is null ? null : FireteamLevels.ClaimSlots(Members(), slots);
        for (var i = 0; i < MercsCompanyEntries.Count; i++)
        {
            MercsCompanyEntries[i].IsIrregular = claimed is not null && i < claimed.Count && !claimed[i];
        }
    }

    // The tracked team's slots plus every wildcard bucket's, or null when no fireteam is tracked.
    private List<FireteamSlot>? TrackedFireteamSlots()
    {
        if (string.IsNullOrWhiteSpace(TrackedFireteamName) ||
            TeamEntries.FirstOrDefault(team => team.ShowTrackingRadioButton &&
                                               string.Equals(team.Name, TrackedFireteamName, StringComparison.OrdinalIgnoreCase)) is not { } tracked)
        {
            return null;
        }

        return tracked.AllowedProfiles
            .Concat(TeamEntries.Where(team => team.IsWildcardBucket).SelectMany(team => team.AllowedProfiles))
            .DistinctBy(slot => $"{slot.Name}|{slot.ResolvedUnitId}|{slot.ResolvedSourceFactionId}", StringComparer.OrdinalIgnoreCase)
            .Select(slot => new FireteamSlot(slot.Name, slot.ResolvedUnitId, slot.ResolvedSourceFactionId, slot.Max))
            .ToList();
    }

    private List<FireteamMember> Members() =>
        MercsCompanyEntries
            .Select(entry => new FireteamMember(entry.BaseUnitName, entry.Name, entry.SourceUnitId, entry.SourceFactionId))
            .ToList();

    protected override void AfterRosterChanged() => ReevaluateTrackedFireteam();

    protected override void AfterCompanyReset() => ReevaluateTrackedFireteam();

    // ── Profiles and troopers ───────────────────────────────────────────────

    /// <summary>Picking a unit from a fireteam-only (FTO) slot shows only its FTO profiles.</summary>
    protected override bool RestrictsProfilesFor(ArmyTeamUnitLimitItem teamItem) => FireteamMatching.IsFtoLabel(teamItem.Name);

    protected override bool IncludesOption(string? optionName) => !RestrictsProfilesToFto || FireteamMatching.IsFtoLabel(optionName);

    /// <summary>Tactical Awareness (on the option, what it includes, or the group's profiles) costs 3 points less.</summary>
    protected override string AdjustOptionDisplayCost(
        UnitProfileGroups profileGroups, ArmyFilterCatalog filters, ProfileGroup group, ProfileOption option, string cost)
    {
        if (!int.TryParse(cost, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return cost;
        }

        var skills = filters.Names(ArmyFilterSection.Skills);
        var hasTacticalAwareness =
            HasTacticalAwareness(profileGroups.GetEntriesWithIncludes(option, ProfileEntryKind.Skills), skills) ||
            group.Profiles.Any(profile => HasTacticalAwareness(profile.Skills, skills));
        return hasTacticalAwareness
            ? Math.Max(0, parsed - TacticalAwarenessDiscount).ToString(CultureInfo.InvariantCulture)
            : cost;
    }

    private static bool HasTacticalAwareness(IEnumerable<ProfileEntry> skillEntries, IReadOnlyDictionary<int, string> skills) =>
        skillEntries.Any(entry =>
            entry.Id is int id &&
            skills.TryGetValue(id, out var name) &&
            name.Contains("tactical awareness", StringComparison.OrdinalIgnoreCase));

    /// <summary>Every trooper has Number 2; the lieutenant also has FT Master.</summary>
    protected override IReadOnlyCollection<string> CommonSkillsFor(
        ArmyUnitSelectionItem unit, ViewerProfileItem profile, IReadOnlyList<string> commonSkills)
    {
        var skills = commonSkills.Where(skill => !string.IsNullOrWhiteSpace(skill)).Select(skill => skill.Trim()).ToList();
        if (!skills.Any(skill => skill.Contains("number 2", StringComparison.OrdinalIgnoreCase)))
        {
            skills.Add(NumberTwoSkill);
        }

        if (profile.IsLieutenant && !skills.Any(skill => skill.Contains("ft master", StringComparison.OrdinalIgnoreCase)))
        {
            skills.Add(FireteamMasterSkill);
        }

        return skills;
    }

    /// <summary>Remembers whether the trooper is normally irregular, for the order badges.</summary>
    protected override void OnEntryBuilt(MercsCompanyEntry entry) => entry.NormallyIrregular = Detail.ShowIrregularOrderIcon;
}
