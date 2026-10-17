using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.ViewModels.Season.Items;
using InfinityMercsApp.Views.Season.Game;

using InfinityMercsApp.Presentation.Season;
namespace InfinityMercsApp.ViewModels.Season;

/// <summary>
/// Chooses which units deploy for a mission. The lieutenant always deploys; at most six slots can be
/// filled (a TAG Company TAG takes two), and four or fewer is an elite deployment.
/// </summary>
public partial class PlayModeViewModel(
    INavigationService navigationService,
    IDialogService dialogs,
    DeploymentRoster roster,
    IAppSettingsProvider? settings = null) : ViewModelBase(navigationService)
{
    public const int MaxSlots = 6;
    public const int MaxEliteSlots = 4;

    private string _companyFilePath = string.Empty;
    private string? _seasonFilePath;
    private bool _loaded;
    private bool? _loadedInInches;

    public ObservableCollection<DeploymentUnitItem> DeploymentUnits { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedUnit), nameof(NoUnitSelected))]
    public partial DeploymentUnitItem? SelectedUnit { get; set; }

    public bool HasSelectedUnit => SelectedUnit is not null;

    public bool NoUnitSelected => SelectedUnit is null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEliteDeployment), nameof(DeploymentStatusText))]
    public partial int DeployedSlotCount { get; set; }

    public bool IsEliteDeployment => DeployedSlotCount is > 0 and <= MaxEliteSlots;

    public string DeploymentStatusText => IsEliteDeployment
        ? $"{DeployedSlotCount} / {MaxSlots} slots · ELITE"
        : $"{DeployedSlotCount} / {MaxSlots} slots";

    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (NavigationParameters.GetString(query, NavigationParameters.CompanyFilePath) is { } companyFilePath)
        {
            _companyFilePath = companyFilePath;
            _loaded = false;
        }

        _seasonFilePath = NavigationParameters.GetString(query, NavigationParameters.SeasonFilePath) ?? _seasonFilePath;
    }

    /// <summary>Loads the company once, and again if the distance units setting has changed since.</summary>
    public override async Task InitializeAsync()
    {
        var inches = SeasonDisplayUnitFormatter.GetShowUnitsInInches(settings);
        if ((_loaded && _loadedInInches == inches) || string.IsNullOrWhiteSpace(_companyFilePath))
        {
            return;
        }

        _loaded = true;
        _loadedInInches = inches;

        foreach (var unit in DeploymentUnits)
        {
            unit.PropertyChanged -= OnUnitPropertyChanged;
        }

        DeploymentUnits.Clear();
        foreach (var unit in await roster.LoadAsync(_companyFilePath, _seasonFilePath, inches))
        {
            unit.PropertyChanged += OnUnitPropertyChanged;
            DeploymentUnits.Add(unit);
        }

        DeployedSlotCount = CountDeployedSlots();
        SelectUnit(DeploymentUnits.FirstOrDefault());
    }

    [RelayCommand]
    private void SelectUnit(DeploymentUnitItem? item)
    {
        foreach (var unit in DeploymentUnits)
        {
            unit.IsSelected = ReferenceEquals(unit, item);
        }

        SelectedUnit = item;
    }

    [RelayCommand]
    private async Task DeployAsync()
    {
        var deployed = DeploymentUnits.Where(u => u.IsChecked).ToList();
        if (deployed.Count == 0)
        {
            await dialogs.ShowAlertAsync("No Units Selected", "Select at least one unit to deploy.");
            return;
        }

        var parameters = NavigationParameters.ForCompany(_companyFilePath, _seasonFilePath);
        parameters[NavigationParameters.DeployedIndices] = deployed.Select(u => u.EntryIndex).ToList();
        parameters[NavigationParameters.EliteDeployment] = IsEliteDeployment;
        await NavigationService.PushAsync(nameof(GameModePage), parameters);
    }

    // Undoes a check that would leave the lieutenant behind or go over the slot limit.
    private void OnUnitPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(DeploymentUnitItem.IsChecked) || sender is not DeploymentUnitItem unit)
        {
            return;
        }

        if (unit is { IsLieutenant: true, IsChecked: false })
        {
            unit.IsChecked = true;
            return;
        }

        if (unit.IsChecked && CountDeployedSlots() > MaxSlots)
        {
            unit.IsChecked = false;
            return;
        }

        DeployedSlotCount = CountDeployedSlots();
    }

    private int CountDeployedSlots() => DeploymentUnits.Where(u => u.IsChecked).Sum(u => u.SlotCost);
}
