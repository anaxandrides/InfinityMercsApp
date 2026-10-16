using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Season.Injuries;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.ViewModels.Company.Viewer;

namespace InfinityMercsApp.ViewModels.Season;

/// <summary>
/// After a contract: an injury check for every trooper taken out, and one injury shed by every injured trooper
/// that rested (and by the captain, deployed or not). Continuing saves both to the season.
/// </summary>
public partial class InjuriesViewModel(
    INavigationService navigationService,
    MissionDebrief debrief,
    IDice dice,
    ISeasonFileService seasonFiles,
    ICompanyFileService companyFiles,
    CompanyRoster roster) : ViewModelBase(navigationService)
{
    private string _companyFilePath = string.Empty;
    private string _seasonFilePath = string.Empty;

    public ObservableCollection<InjuryCheckItem> Injuries { get; } = [];

    public ObservableCollection<InjuryRecoveryItem> Recoveries { get; } = [];

    [ObservableProperty]
    private bool _hasInjuryChecks;

    [ObservableProperty]
    private bool _hasRecoveries;

    /// <summary>True once every injury check is settled.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private bool _canContinue = true;

    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _companyFilePath = NavigationParameters.GetString(query, NavigationParameters.CompanyFilePath) ?? _companyFilePath;
        _seasonFilePath = NavigationParameters.GetString(query, NavigationParameters.SeasonFilePath) ?? _seasonFilePath;
    }

    public override async Task InitializeAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        var seasonFile = await seasonFiles.LoadAsync(_seasonFilePath);
        IReadOnlyList<Injury> Carried(int entryIndex) =>
            seasonFile?.Troopers.GetValueOrDefault(entryIndex)?.Injuries ?? [];

        foreach (var injury in debrief.PendingInjuries)
        {
            Injuries.Add(new InjuryCheckItem(injury, Carried(injury.EntryIndex), dice, OnSettled));
        }

        foreach (var unit in await CompanyUnitsAsync())
        {
            var carried = Carried(unit.EntryIndex);
            var deployed = debrief.Units.Any(deployedUnit => deployedUnit.EntryIndex == unit.EntryIndex);
            if (InjuryRules.Recovers(deployed, unit.IsLieutenant, carried.Count > 0))
            {
                Recoveries.Add(new InjuryRecoveryItem(unit.EntryIndex, unit.Name, unit.IsLieutenant, carried));
            }
        }

        HasInjuryChecks = Injuries.Count > 0;
        HasRecoveries = Recoveries.Count > 0;
        CanContinue = AllSettled();
    }

    private async Task<IReadOnlyList<CompanyViewerUnitListItem>> CompanyUnitsAsync()
    {
        try
        {
            return companyFiles.Exists(_companyFilePath) && await companyFiles.LoadAsync(_companyFilePath) is { } company
                ? roster.UnitsOf(company)
                : [];
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Reading the company for injury recovery failed: {ex.Message}");
            return [];
        }
    }

    // An injured trooper's experience card shows the injury and its XP.
    private void OnSettled(InjuryCheckItem item)
    {
        if (item.IsInjured && item.SelectedInjury is { } injury
            && debrief.Units.FirstOrDefault(u => u.EntryIndex == item.Injury.EntryIndex) is { } unit)
        {
            unit.GainedInjury = true;
            unit.InjuryName = injury.Label;
        }

        CanContinue = AllSettled();
    }

    private bool AllSettled() => Injuries.All(i => i.IsSettled);

    /// <summary>Sheds the chosen injuries, then adds the new ones (so a fresh injury is never the one shed).</summary>
    [RelayCommand(CanExecute = nameof(CanContinue))]
    private async Task ContinueAsync()
    {
        var shed = Recoveries
            .Select(recovery => recovery.SelectedInjury is { } entry ? new TrooperInjury(recovery.EntryIndex, entry.Injury) : null)
            .OfType<TrooperInjury>()
            .ToList();
        var taken = Injuries
            .Select(check => check is { IsInjured: true, SelectedInjury: { } entry } ? new TrooperInjury(check.Injury.EntryIndex, entry.Injury) : null)
            .OfType<TrooperInjury>()
            .ToList();

        if (shed.Count > 0 || taken.Count > 0)
        {
            await seasonFiles.UpdateAsync(_seasonFilePath, season =>
            {
                foreach (var recovery in shed)
                {
                    season.TrooperFor(recovery.EntryIndex).Injuries.Remove(recovery.Injury);
                }

                foreach (var injury in taken)
                {
                    var trooper = season.TrooperFor(injury.EntryIndex);
                    if (!trooper.Injuries.Contains(injury.Injury))
                    {
                        trooper.Injuries.Add(injury.Injury);
                    }
                }
            });
        }

        debrief.PendingInjuries.Clear();
        await NavigationService.PushAsync(
            "MissionOutcomePage",
            NavigationParameters.ForCompany(_companyFilePath, _seasonFilePath));
    }

    private sealed record TrooperInjury(int EntryIndex, Injury Injury);
}
