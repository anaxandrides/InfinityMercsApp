using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Season.Byes;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Presentation.Season;
using InfinityMercsApp.Presentation.Season.Bye;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.ViewModels.Season.Bye.Items;

namespace InfinityMercsApp.ViewModels.Season.Bye;

/// <summary>
/// A contract the company cannot play. The player shares the bye's XP (up to 4 troopers at 7 XP, or up to 6 at
/// 5 XP) and writes off one injury. Confirming records the bye as the next round, then goes on to downtime, where
/// any trooper can take part since nobody was deployed.
/// </summary>
public partial class ByeRoundViewModel(
    INavigationService navigationService,
    IDialogService dialogs,
    ISeasonFileService seasonFiles,
    DeploymentRoster roster,
    MissionDebrief debrief,
    IAppSettingsProvider? settings = null) : ViewModelBase(navigationService)
{
    private string _companyFilePath = string.Empty;
    private string _seasonFilePath = string.Empty;

    public ObservableCollection<ByeTrooperItem> Troopers { get; } = [];

    public ObservableCollection<ByeInjuryOption> InjuryOptions { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFourTroopers), nameof(IsSixTroopers))]
    public partial ByeXpSplit Split { get; set; } = ByeXpSplit.FourTroopers;

    public bool IsFourTroopers => Split == ByeXpSplit.FourTroopers;

    public bool IsSixTroopers => Split == ByeXpSplit.SixTroopers;

    /// <summary>The round the bye stands in for.</summary>
    [ObservableProperty]
    public partial int RoundNumber { get; set; } = 1;

    /// <summary>The round's credits plus the bye's 12.</summary>
    [ObservableProperty]
    public partial int Credits { get; set; }

    /// <summary>"2 / 4 troopers · 7 XP each".</summary>
    [ObservableProperty]
    public partial string SelectionText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    public partial bool IsValid { get; set; } = true;

    [ObservableProperty]
    public partial ByeInjuryOption? SelectedInjury { get; set; }

    [ObservableProperty]
    public partial bool HasInjuries { get; set; }

    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _companyFilePath = NavigationParameters.GetString(query, NavigationParameters.CompanyFilePath) ?? _companyFilePath;
        _seasonFilePath = NavigationParameters.GetString(query, NavigationParameters.SeasonFilePath) ?? _seasonFilePath;
    }

    /// <summary>Lists the troopers and the injuries they carry, with nobody ticked.</summary>
    public override async Task InitializeAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        var season = await seasonFiles.LoadAsync(_seasonFilePath);
        RoundNumber = SeasonRounds.Current(season) + 1;
        Credits = SeasonEconomy.MissionCredits(RoundNumber) + SeasonEconomy.ByeCredits(isBye: true);

        Troopers.Clear();
        InjuryOptions.Clear();
        var units = await roster.LoadAsync(_companyFilePath, _seasonFilePath, SeasonDisplayUnitFormatter.GetShowUnitsInInches(settings));
        // A Lone Wolf's Aspects are the Lone Wolf: XP and injuries are its alone.
        foreach (var unit in units.Where(unit => unit.AspectOf is null))
        {
            Troopers.Add(new ByeTrooperItem(unit));
            foreach (var injury in season?.Troopers.GetValueOrDefault(unit.EntryIndex)?.Injuries ?? [])
            {
                InjuryOptions.Add(new ByeInjuryOption(unit.EntryIndex, unit.Name, injury));
            }
        }

        HasInjuries = InjuryOptions.Count > 0;
        SelectedInjury = InjuryOptions.FirstOrDefault();
        Recalculate();
    }

    partial void OnSplitChanged(ByeXpSplit value) => Recalculate();

    [RelayCommand]
    private void SelectSplit(ByeXpSplit split) => Split = split;

    /// <summary>Ticks or unticks a trooper; one more than the split allows is not ticked.</summary>
    [RelayCommand]
    private void ToggleTrooper(ByeTrooperItem trooper)
    {
        if (!trooper.IsSelected && Choice().Troopers.Count >= ByeRound.MostTroopers(Split))
        {
            return;
        }

        trooper.IsSelected = !trooper.IsSelected;
        Recalculate();
    }

    /// <summary>After confirming, records the bye and goes on to downtime with every trooper available.</summary>
    [RelayCommand(CanExecute = nameof(IsValid))]
    private async Task ConfirmAsync()
    {
        var choice = Choice();
        if (!await dialogs.ConfirmAsync("Take a Bye", $"Record round {RoundNumber} as a bye?"))
        {
            return;
        }

        if (!await seasonFiles.UpdateAsync(_seasonFilePath, season => ByeRound.Apply(season, choice)))
        {
            await dialogs.ShowAlertAsync("Bye Not Recorded", SeasonMessages.NotSaved);
            return;
        }

        debrief.Units = Troopers.Select(trooper => trooper.Unit.ToResult(deploymentXp: 0, isEliteDeployment: false, wasDeployed: false)).ToList();
        debrief.Troopers = debrief.Units;
        debrief.PendingInjuries.Clear();
        await NavigationService.PushAsync(
            "DowntimePage",
            NavigationParameters.ForCompany(_companyFilePath, _seasonFilePath));
    }

    private ByeRoundChoice Choice() => new(
        Split,
        Troopers.Where(trooper => trooper.IsSelected).Select(trooper => new ByeTrooper(trooper.EntryIndex, trooper.Name)).ToList(),
        SelectedInjury is { } injury ? new InjuryWriteOff(injury.EntryIndex, injury.Injury) : null);

    private void Recalculate()
    {
        var choice = Choice();
        IsValid = ByeRound.IsValid(choice);
        SelectionText = $"{choice.Troopers.Count} / {ByeRound.MostTroopers(Split)} troopers · {ByeRound.XpEach(Split)} XP each";
    }
}
