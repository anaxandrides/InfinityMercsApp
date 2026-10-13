using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Services;

namespace InfinityMercsApp.ViewModels;

/// <summary>A perk the chosen configuration qualifies for.</summary>
public sealed record QualifiedPerkItem(string PerkText, string Meta, string Granted);

/// <summary>
/// The unit browser plus the perks a chosen configuration qualifies for, to check the perk rules.
/// Choosing a unit selects its first configuration.
/// </summary>
public partial class PerkTesterViewModel : UnitEncyclopediaViewModel
{
    private ViewerProfileItem? _selectedProfile;

    public PerkTesterViewModel(
        INavigationService navigationService,
        IArmyDataService armyData,
        UnitDetailViewModel detail,
        FactionLogoCacheService? logoCache = null)
        : base(navigationService, armyData, detail, logoCache)
    {
        PropertyChanged += OnOwnPropertyChanged;
        Detail.Profiles.CollectionChanged += OnProfilesChanged;
    }

    public ObservableCollection<QualifiedPerkItem> QualifiedPerks { get; } = [];

    [RelayCommand]
    private void SelectProfile(ViewerProfileItem? profile)
    {
        if (profile is null)
        {
            return;
        }

        foreach (var entry in Detail.Profiles)
        {
            entry.IsSelected = ReferenceEquals(entry, profile);
        }

        _selectedProfile = profile;
        ShowQualifiedPerks(profile);
    }

    private void OnOwnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedUnit))
        {
            ClearSelection();
        }
    }

    private void OnProfilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Detail.Profiles.Count == 0)
        {
            ClearSelection();
        }
        else if (_selectedProfile is null || !Detail.Profiles.Contains(_selectedProfile))
        {
            SelectProfile(Detail.Profiles[0]);
        }
    }

    private void ClearSelection()
    {
        _selectedProfile = null;
        foreach (var profile in Detail.Profiles)
        {
            profile.IsSelected = false;
        }

        QualifiedPerks.Clear();
    }

    private void ShowQualifiedPerks(ViewerProfileItem profile)
    {
        var skills = SummaryValues(Detail.SpecialSkillsSummary).Concat(ListValues(profile.UniqueSkills)).ToList();
        var equipment = SummaryValues(Detail.EquipmentSummary).Concat(ListValues(profile.UniqueEquipment)).ToList();
        var weapons = ListValues(profile.RangedWeapons).Concat(ListValues(profile.MeleeWeapons)).ToList();
        var characteristics = ListValues(profile.Characteristics).ToList();
        var includeMechaTrack = SelectedUnit?.Subtitle?.Contains("tag", StringComparison.OrdinalIgnoreCase) == true;

        QualifiedPerks.Clear();
        foreach (var id in CompanyPerkOwnershipResolver.ResolveOwnedPerkNodeIds(skills, equipment, weapons, characteristics, includeMechaTrack))
        {
            if (Describe(id) is { } perk)
            {
                QualifiedPerks.Add(perk);
            }
        }
    }

    /// <summary>A perk node as listed: its text, "List | Track n Tier n | id", and what it grants.</summary>
    public static QualifiedPerkItem? Describe(string nodeId)
    {
        if (CompanyPerkCatalog.FindById(nodeId) is not { } perk)
        {
            return null;
        }

        var markerIndex = nodeId.IndexOf("-track-", StringComparison.OrdinalIgnoreCase);
        var listId = markerIndex > 0 ? nodeId[..markerIndex] : "unknown";
        var listName = CompanyPerkCatalog.FindPerkListCatalogEntry(listId)?.Name ?? listId;
        var trackTier = Regex.Match(nodeId, @"-track-(?<track>\d+)-tier-(?<tier>\d+)", RegexOptions.IgnoreCase);
        var track = trackTier.Success ? trackTier.Groups["track"].Value : "0";
        var tier = trackTier.Success ? trackTier.Groups["tier"].Value : "0";

        return new QualifiedPerkItem(
            string.IsNullOrWhiteSpace(perk.Description) ? perk.Name : perk.Description,
            $"{listName} | Track {track} Tier {tier} | {nodeId}",
            GrantedText(FindNode(nodeId)));
    }

    /// <summary>"Gives: Name (detail), …", or "Gives: -".</summary>
    public static string GrantedText(PerkNode? node)
    {
        var parts = (node?.SkillsEquipmentGained ?? [])
            .Select(gained => (Name: gained.Item1?.Trim(), Extra: gained.Item2?.Trim()))
            .Where(gained => !string.IsNullOrWhiteSpace(gained.Name))
            .Select(gained => string.IsNullOrWhiteSpace(gained.Extra) ? gained.Name! : $"{gained.Name} ({gained.Extra})")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return parts.Count == 0 ? "Gives: -" : $"Gives: {string.Join(", ", parts)}";
    }

    private static PerkNode? FindNode(string id) =>
        CompanyPerkCatalog.GetPerkNodeLists()
            .SelectMany(list => list.Roots)
            .Select(root => root.FindById(id))
            .FirstOrDefault(match => match is not null);

    private static IEnumerable<string> SummaryValues(string summary)
    {
        var separatorIndex = summary.IndexOf(':', StringComparison.Ordinal);
        return ListValues(separatorIndex >= 0 ? summary[(separatorIndex + 1)..] : summary);
    }

    private static IEnumerable<string> ListValues(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Where(value => !string.Equals(value, "-", StringComparison.OrdinalIgnoreCase));
}
