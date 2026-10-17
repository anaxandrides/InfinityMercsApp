using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Infrastructure.Providers.Metadata;
using InfinityMercsApp.Presentation.Armory;
using InfinityMercsApp.Presentation.Layouts;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels.Armory;

/// <summary>Searchable list of weapons, each opening its profiles in a detail pane.</summary>
public partial class ArmoryViewModel(
    INavigationService navigationService,
    IMetadataProvider metadataProvider) : AdaptiveViewModelBase<SplitPaneLayout>(navigationService, mode => SplitPaneLayout.For(mode, Sizes))
{
    private static readonly SplitPaneSizes Sizes = new(320, 340, 360, 820);

    public ObservableCollection<WeaponGroup> WeaponGroups { get; } = [];

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedWeaponName), nameof(HasSelectedWeapon), nameof(NoWeaponSelected))]
    public partial WeaponGroup? SelectedWeaponGroup { get; set; }

    /// <summary>Whether the detail pane replaces the list on narrow screens.</summary>
    [ObservableProperty]
    public partial bool IsShowingDetail { get; set; }

    [ObservableProperty]
    public partial bool IsListPaneVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool IsDetailPaneVisible { get; set; }

    protected override void OnLayoutApplied() => ShowPanes();

    partial void OnIsShowingDetailChanged(bool value) => ShowPanes();

    private void ShowPanes()
    {
        IsListPaneVisible = Layout.ShowsList(IsShowingDetail);
        IsDetailPaneVisible = Layout.ShowsDetail(IsShowingDetail);
    }

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
