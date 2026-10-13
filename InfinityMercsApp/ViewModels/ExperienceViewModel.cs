using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Services;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels;

/// <summary>One deployed trooper's XP card; exactly one trooper can be the MVP.</summary>
public partial class ExperienceUnitItem(ExperienceUnitResult unit) : ObservableObject
{
    public ExperienceUnitResult Unit { get; } = unit;

    public string Name => Unit.Name;

    public string? LogoSource { get; } = UnitLogo.Source(unit.CachedLogoPath, unit.PackagedLogoPath);

    public IReadOnlyList<XpLine> XpLines { get; } = MissionExperience.Lines(unit);

    public int MvpXp => MissionExperience.MvpXp;

    public string TotalText => $"Total: {Unit.TotalXp} XP";

    public bool IsMvp
    {
        get => Unit.IsMvp;
        set
        {
            if (Unit.IsMvp == value)
            {
                return;
            }

            Unit.IsMvp = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TotalText));
        }
    }
}

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

        ConfirmCommand.NotifyCanExecuteChanged();
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

        ConfirmCommand.NotifyCanExecuteChanged();
    }

    private bool CanConfirm() => Units.Count == 0 || Units.Any(u => u.IsMvp);

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private async Task ConfirmAsync()
    {
        var results = Units
            .Select(u => u.Unit)
            .Where(u => u.TotalXp > 0)
            .Select(u => new SeasonMissionUnitResult { UnitName = u.Name, XpGained = u.TotalXp })
            .ToList();

        await seasonFiles.UpdateLatestRoundAsync(_seasonFilePath, round => round.MissionResults.UnitResults = results);

        await NavigationService.PushAsync(
            "DowntimePage",
            NavigationParameters.ForCompany(_companyFilePath, _seasonFilePath));
    }
}
