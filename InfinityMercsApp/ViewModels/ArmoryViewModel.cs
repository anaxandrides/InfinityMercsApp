using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels;

/// <summary>A weapon and all of its firing modes.</summary>
public sealed record WeaponGroup(string Name, IReadOnlyList<Weapon> Modes);

/// <summary>Searchable list of weapons, each opening its profiles in a detail pane.</summary>
public partial class ArmoryViewModel(
    INavigationService navigationService,
    IMetadataProvider metadataProvider) : ViewModelBase(navigationService)
{
    public ObservableCollection<WeaponGroup> WeaponGroups { get; } = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedWeaponName), nameof(HasSelectedWeapon), nameof(NoWeaponSelected))]
    private WeaponGroup? _selectedWeaponGroup;

    /// <summary>Whether the detail pane replaces the list on narrow screens.</summary>
    [ObservableProperty]
    private bool _isShowingDetail;

    public string SelectedWeaponName => SelectedWeaponGroup?.Name ?? "Select a weapon";

    public bool HasSelectedWeapon => SelectedWeaponGroup is not null;

    public bool NoWeaponSelected => SelectedWeaponGroup is null;

    public override Task InitializeAsync()
    {
        if (!IsInitialized)
        {
            LoadWeapons();
        }

        return Task.CompletedTask;
    }

    partial void OnSearchTextChanged(string value) => LoadWeapons();

    partial void OnSelectedWeaponGroupChanged(WeaponGroup? value) => IsShowingDetail = value is not null;

    [RelayCommand]
    private void CloseDetail() => IsShowingDetail = false;

    /// <summary>Weapons grouped by trimmed name (case-insensitive), modes in order, groups alphabetical.</summary>
    public static IReadOnlyList<WeaponGroup> GroupByName(IEnumerable<Weapon> weapons) =>
        weapons
            .GroupBy(w => w.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new WeaponGroup(g.Key, g.OrderBy(w => w.Mode ?? string.Empty).ToList()))
            .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private void LoadWeapons()
    {
        var weapons = string.IsNullOrWhiteSpace(SearchText)
            ? metadataProvider.GetAllWeapons()
            : metadataProvider.SearchWeaponsByName(SearchText);

        WeaponGroups.Clear();
        foreach (var group in GroupByName(weapons))
        {
            WeaponGroups.Add(group);
        }
    }
}
