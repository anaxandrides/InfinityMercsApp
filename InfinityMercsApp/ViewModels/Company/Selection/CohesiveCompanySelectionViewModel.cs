using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.Teams;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using CCFactionFireteamValidityRecord = InfinityMercsApp.Domain.Models.Army.CCFactionFireteamValidityRecord;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.ViewModels.Company.Selection;

/// <summary>
/// A Cohesive company: one sectorial that can field a Core fireteam within the points limit. The player
/// tracks one fireteam; its level gives a bonus, and troopers outside it are irregular. Every trooper must be
/// able to join it. Troopers get Number 2 (and the lieutenant FT Master), and Tactical Awareness costs 3
/// points less. The fireteam is saved with the company and fixed from then on, so recruiting keeps it.
/// </summary>
public sealed partial class CohesiveCompanySelectionViewModel : CompanySelectionViewModel
{
    public const string NumberTwoSkill = "Number 2";
    public const string FireteamMasterSkill = "FT Master";
    public const int TacticalAwarenessPointsOff = 3;

    private readonly Dictionary<int, HashSet<string>> _validCoreFireteamsByFaction = [];
    private HashSet<string> _validCoreTeamNames = new(StringComparer.OrdinalIgnoreCase);
    private string _trackedFireteamToRestore = string.Empty;
    private bool _isUpdatingTrackedTeam;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TrackedFireteamNameDisplay))]
    public partial string TrackedFireteamName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TrackedFireteamLevelBonusText), nameof(HasTrackedFireteamLevelBonus), nameof(TrackedFireteamLevelIcon))]
    public partial int TrackedFireteamLevel { get; set; }

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
            _validCoreFireteamsByFaction[row.FactionId] = CoreFireteamEligibility.ToValidCoreFireteamSet(row.ValidCoreFireteams);
        }

        return ordered.Where(faction => cachedByFaction.TryGetValue(faction.Id, out var row) && row.HasValidCoreFireteams).ToList();
    }

    private List<string> EvaluateValidCoreFireteams(FactionRecord faction, int maxCost, CancellationToken cancellationToken) =>
        Services.CoreFireteams.ValidCoreFireteams(faction.Id, ActiveUnitFilter, maxCost, cancellationToken);

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

    // While recruiting, the company's saved fireteam can't be swapped for another.
    private void OnTeamPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_isUpdatingTrackedTeam &&
            e.PropertyName == nameof(ArmyTeamListItem.IsTrackedTeam) &&
            sender is ArmyTeamListItem { IsTrackedTeam: true, ShowTrackingRadioButton: true } team)
        {
            SetTrackedFireteam(IsRecruiting ? TrackedFireteamName : team.Name);
        }
    }

    protected override string CoreFireteam => TrackedFireteamName;

    protected override void OnPreparingRecruitment(SavedCompanyFile company) =>
        TrackedFireteamName = _trackedFireteamToRestore = company.CoreFireteam;

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

        // Without a tracked fireteam nobody is irregular, and each trooper's profile decides.
        var claimed = slots is null ? null : FireteamLevels.ClaimSlots(Members(), slots);
        for (var i = 0; i < MercsCompanyEntries.Count; i++)
        {
            var entry = MercsCompanyEntries[i];
            entry.IsIrregular = claimed is not null && i < claimed.Count && !claimed[i];
            entry.IrregularInCompany = claimed is null ? null : entry.IsIrregular;
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

    /// <summary>Every trooper must be able to join the core fireteam; anyone can join while none is tracked.</summary>
    protected override bool CanAddProfile(ArmyUnitSelectionItem unit) =>
        TrackedFireteamSlots() is not { } slots ||
        FireteamLevels.CanJoin(new FireteamMember(unit.Name, unit.Name, unit.Id, unit.SourceFactionId), slots);

    protected override string? CannotAddMessage(ArmyUnitSelectionItem unit) =>
        $"{unit.Name} can't join {TrackedFireteamName}, and every trooper in a Cohesive company must be able to.";

    /// <summary>Picking a unit from a fireteam-only (FTO) slot shows only its FTO profiles.</summary>
    protected override bool RestrictsProfilesFor(ArmyTeamUnitLimitItem teamItem) => FireteamMatching.IsFtoLabel(teamItem.Name);

    /// <summary>Tactical Awareness (on the option, what it includes, or the group's profiles) costs 3 points less.</summary>
    protected override int TacticalAwarenessDiscount => TacticalAwarenessPointsOff;

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
    protected override void OnEntryBuilt(MercsCompanyEntry entry) => entry.NormallyIrregular = Detail.Icons.Irregular;
}
