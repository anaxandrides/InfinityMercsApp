using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.ViewModels.Company.Selection.Items;

namespace InfinityMercsApp.ViewModels.Company.Selection;

/// <summary>
/// A Cohesive company's Core fireteam: the one fireteam the player tracks in the fireteam list, its level and the
/// bonus that gives, and which troopers are irregular for being outside it. Once the company is founded the
/// fireteam is fixed, so a recruiting company can't swap it.
/// </summary>
public sealed partial class CoreFireteamTracker : ObservableObject, IFireteamTracking
{
    private readonly ObservableCollection<ArmyTeamListItem> _teams;
    private readonly ObservableCollection<MercsCompanyEntry> _entries;
    private IReadOnlyDictionary<int, HashSet<string>> _validTeamsByFaction = new Dictionary<int, HashSet<string>>();
    private HashSet<string> _validTeamNames = new(StringComparer.OrdinalIgnoreCase);
    private string _toRestore = string.Empty;
    private bool _isLocked;
    private bool _isUpdating;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NameDisplay))]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LevelBonusText), nameof(HasLevelBonus), nameof(LevelIcon))]
    public partial int Level { get; set; }

    /// <param name="teams">
    /// The fireteam list, whose radio buttons pick the tracked fireteam; each item is given this tracker as its
    /// <see cref="ArmyTeamListItem.Tracking"/>.
    /// </param>
    /// <param name="entries">The company's troopers, marked irregular when outside the fireteam.</param>
    public CoreFireteamTracker(ObservableCollection<ArmyTeamListItem> teams, ObservableCollection<MercsCompanyEntry> entries)
    {
        _teams = teams;
        _entries = entries;
    }

    public string NameDisplay => string.IsNullOrWhiteSpace(Name) ? "Select fireteam" : Name;

    /// <summary>The bonus the fireteam's level gives, when it gives one.</summary>
    public string? LevelBonusText => FireteamLevels.BonusText(Level);

    public bool HasLevelBonus => !string.IsNullOrEmpty(LevelBonusText);

    /// <summary>The badge for the fireteam's level, or none below level 1.</summary>
    public string? LevelIcon => Level is >= 1 and <= FireteamLevels.MaxLevel
        ? $"SVGCache/NonCBIcons/Fireteam/noun-team-{Level}.svg"
        : null;

    /// <summary>Which Core fireteams each sectorial can field within the points limit.</summary>
    public void SetValidTeams(IReadOnlyDictionary<int, HashSet<string>> validTeamsByFaction) => _validTeamsByFaction = validTeamsByFaction;

    /// <summary>Takes up the fireteam a recruiting company was founded with; it can't be swapped from then on.</summary>
    public void Fix(string savedName)
    {
        Name = _toRestore = savedName;
        _isLocked = true;
    }

    /// <summary>Remembers the tracked fireteam while the fireteam list reloads.</summary>
    public void RememberForReload() => _toRestore = Name;

    /// <summary>The fireteams the source factions can field, just before the list is built.</summary>
    public void UseFactions(IEnumerable<int> factionIds) =>
        _validTeamNames = factionIds
            .SelectMany(id => _validTeamsByFaction.TryGetValue(id, out var names) ? names : [])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Only the Core fireteams the company can field; all of them when none were evaluated.</summary>
    public bool Lists(MergedFireteam team) => _validTeamNames.Count == 0 || _validTeamNames.Contains(team.Name);

    /// <summary>Tracks the fireteam tracked before the reload, or the first one listed.</summary>
    public void RestoreAfterReload() => Restore(_toRestore);

    /// <summary>Tracks the first fireteam listed, after the list failed to load.</summary>
    public void RestoreDefault() => Restore(string.Empty);

    /// <summary>Tracks <paramref name="teamName"/> and re-evaluates its level and who is irregular.</summary>
    public void Track(string? teamName)
    {
        var normalized = teamName?.Trim() ?? string.Empty;
        _isUpdating = true;
        try
        {
            foreach (var team in _teams)
            {
                team.IsTrackedTeam = team.ShowTrackingRadioButton &&
                                     normalized.Length > 0 &&
                                     string.Equals(team.Name, normalized, StringComparison.OrdinalIgnoreCase);
            }
        }
        finally
        {
            _isUpdating = false;
        }

        Name = normalized;
        Reevaluate();
    }

    /// <summary>The fireteam's level, and each trooper's irregular flag: without a tracked fireteam, nobody is.</summary>
    public void Reevaluate()
    {
        var slots = Slots();
        Level = slots is null ? 0 : Math.Clamp(FireteamLevels.Level(Members(), slots), 0, FireteamLevels.MaxLevel);

        var claimed = slots is null ? null : FireteamLevels.ClaimSlots(Members(), slots);
        for (var i = 0; i < _entries.Count; i++)
        {
            _entries[i].IsIrregular = claimed is not null && i < claimed.Count && !claimed[i];
        }
    }

    /// <summary>Whether <paramref name="unit"/> can join the fireteam; anyone can while none is tracked.</summary>
    public bool CanJoin(ArmyUnitSelectionItem unit) =>
        Slots() is not { } slots ||
        FireteamLevels.CanJoin(new FireteamMember(unit.Name, unit.Name, unit.Id, unit.SourceFactionId), slots);

    /// <summary>The tracked fireteam's places plus every wildcard bucket's, or null when no fireteam is tracked.</summary>
    public List<FireteamSlot>? Slots()
    {
        if (string.IsNullOrWhiteSpace(Name) ||
            _teams.FirstOrDefault(team => team.ShowTrackingRadioButton &&
                                          string.Equals(team.Name, Name, StringComparison.OrdinalIgnoreCase)) is not { } tracked)
        {
            return null;
        }

        return tracked.AllowedProfiles
            .Concat(_teams.Where(team => team.IsWildcardBucket).SelectMany(team => team.AllowedProfiles))
            .DistinctBy(slot => $"{slot.Name}|{slot.ResolvedUnitId}|{slot.ResolvedSourceFactionId}", StringComparer.OrdinalIgnoreCase)
            .Select(slot => new FireteamSlot(slot.Name, slot.ResolvedUnitId, slot.ResolvedSourceFactionId, slot.Max))
            .ToList();
    }

    private void Restore(string? name)
    {
        var match = string.IsNullOrWhiteSpace(name)
            ? null
            : _teams.FirstOrDefault(team => team.ShowTrackingRadioButton && string.Equals(team.Name, name, StringComparison.OrdinalIgnoreCase));
        Track(match?.Name ?? DefaultName());
    }

    private string DefaultName() =>
        (_teams.FirstOrDefault(team => team.ShowTrackingRadioButton && team.IsVisible)
         ?? _teams.FirstOrDefault(team => team.ShowTrackingRadioButton))?.Name
        ?? string.Empty;

    /// <summary>Ticking a fireteam's radio button tracks it; a recruiting company keeps the one it was founded with.</summary>
    public void Ticked(ArmyTeamListItem team)
    {
        if (!_isUpdating && team is { IsTrackedTeam: true, ShowTrackingRadioButton: true })
        {
            Track(_isLocked ? Name : team.Name);
        }
    }

    private List<FireteamMember> Members() =>
        _entries
            .Select(entry => new FireteamMember(entry.BaseUnitName, entry.Name, entry.SourceUnitId, entry.SourceFactionId))
            .ToList();
}
