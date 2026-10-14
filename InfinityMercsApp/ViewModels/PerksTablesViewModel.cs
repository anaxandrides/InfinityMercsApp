using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Models.Perks;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels;

/// <summary>Picks a perk list and lays it out as a table of tracks by tiers.</summary>
public partial class PerksTablesViewModel : ViewModelBase
{
    public PerksTablesViewModel(INavigationService navigationService) : base(navigationService)
    {
        PerkTableOptions = PerkTable.Options();
        SelectedPerkTableOption = PerkTableOptions.FirstOrDefault();
    }

    public IReadOnlyList<PerkTableOption> PerkTableOptions { get; }

    [ObservableProperty]
    private PerkTableOption? _selectedPerkTableOption;

    /// <summary>The selected list's table, or null when nothing is selected.</summary>
    [ObservableProperty]
    private PerkTable? _table;

    partial void OnSelectedPerkTableOptionChanged(PerkTableOption? value)
    {
        Table = value is null ? null : PerkTable.ForList(value.ListName);
    }
}
