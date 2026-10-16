using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.ViewModels.Season.Items;

namespace InfinityMercsApp.ViewModels.Season;

/// <summary>
/// Shows the XP each deployed trooper earned, lets the player pick the MVP, and records the XP on the
/// latest season round.
/// </summary>
public partial class ExperienceViewModel(
    INavigationService navigationService,
    ISeasonFileService seasonFiles,
    MissionDebrief debrief) : ViewModelBase(navigationService)
{
    private string _companyFilePath = string.Empty;
    private string _seasonFilePath = string.Empty;

    public ObservableCollection<ExperienceUnitItem> Units { get; } = [];

    /// <summary>True once an MVP is picked, or when nobody was deployed.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    public partial bool CanConfirm { get; set; } = true;

    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _companyFilePath = NavigationParameters.GetString(query, NavigationParameters.CompanyFilePath) ?? _companyFilePath;
        _seasonFilePath = NavigationParameters.GetString(query, NavigationParameters.SeasonFilePath) ?? _seasonFilePath;
    }

    public override Task InitializeAsync()
    {
        if (IsInitialized)
        {
            return Task.CompletedTask;
        }

        foreach (var unit in debrief.Units)
        {
            var item = new ExperienceUnitItem(unit);
            item.PropertyChanged += OnUnitPropertyChanged;
            Units.Add(item);
        }

        CanConfirm = HasMvpOrNobody();
        return Task.CompletedTask;
    }

    // Choosing an MVP clears it from everyone else.
    private void OnUnitPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ExperienceUnitItem.IsMvp) || sender is not ExperienceUnitItem chosen)
        {
            return;
        }

        if (chosen.IsMvp)
        {
            foreach (var other in Units.Where(u => !ReferenceEquals(u, chosen)))
            {
                other.IsMvp = false;
            }
        }

        CanConfirm = HasMvpOrNobody();
    }

    private bool HasMvpOrNobody() => Units.Count == 0 || Units.Any(u => u.IsMvp);

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private async Task ConfirmAsync()
    {
        var results = Units
            .Select(u => u.Unit)
            .Where(u => u.TotalXp > 0)
            .Select(u => new SeasonMissionUnitResult { EntryIndex = u.EntryIndex, UnitName = u.Name, XpGained = u.TotalXp })
            .ToList();

        await seasonFiles.UpdateLatestRoundAsync(_seasonFilePath, round => round.MissionResults.UnitResults = results);

        await NavigationService.PushAsync(
            "DowntimePage",
            NavigationParameters.ForCompany(_companyFilePath, _seasonFilePath));
    }
}
