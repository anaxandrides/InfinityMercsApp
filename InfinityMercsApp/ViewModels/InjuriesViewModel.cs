using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Services;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels;

/// <summary>Injury checks for every trooper taken out; continuing needs all of them settled.</summary>
public partial class InjuriesViewModel(
    INavigationService navigationService,
    MissionDebrief debrief,
    IDice dice) : ViewModelBase(navigationService)
{
    private string _companyFilePath = string.Empty;
    private string _seasonFilePath = string.Empty;

    public ObservableCollection<InjuryCheckItem> Injuries { get; } = [];

    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _companyFilePath = NavigationParameters.GetString(query, NavigationParameters.CompanyFilePath) ?? _companyFilePath;
        _seasonFilePath = NavigationParameters.GetString(query, NavigationParameters.SeasonFilePath) ?? _seasonFilePath;
    }

    public override Task InitializeAsync()
    {
        if (!IsInitialized)
        {
            foreach (var injury in debrief.PendingInjuries)
            {
                Injuries.Add(new InjuryCheckItem(injury, dice, OnSettled));
            }

            ContinueCommand.NotifyCanExecuteChanged();
        }

        return Task.CompletedTask;
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

        ContinueCommand.NotifyCanExecuteChanged();
    }

    private bool CanContinue() => Injuries.All(i => i.IsSettled);

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private Task ContinueAsync()
    {
        debrief.PendingInjuries.Clear();
        return NavigationService.PushAsync(
            "MissionOutcomePage",
            NavigationParameters.ForCompany(_companyFilePath, _seasonFilePath));
    }
}
