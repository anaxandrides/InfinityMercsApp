using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Import;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels;

/// <summary>The display-units preference, and a check for army data updates like the one at startup.</summary>
public partial class SettingsViewModel(
    INavigationService navigationService,
    IAppSettingsProvider appSettings,
    IArmyDataUpdater updater,
    IDialogService dialogs) : ViewModelBase(navigationService)
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowUnitsInCentimeters))]
    public partial bool ShowUnitsInInches { get; set; } = true;

    [ObservableProperty]
    public partial string UpdateStatus { get; set; } = "No update run yet.";

    [ObservableProperty]
    public partial string UpdateProgressMessage { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateAllDataCommand))]
    public partial bool IsUpdateInProgress { get; set; }

    // Loading the stored preference must not write it straight back.
    private bool _loading;

    public bool ShowUnitsInCentimeters
    {
        get => !ShowUnitsInInches;
        set => ShowUnitsInInches = !value;
    }

    public override Task InitializeAsync()
    {
        try
        {
            _loading = true;
            ShowUnitsInInches = appSettings.GetShowUnitsInInches();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Loading settings failed: {ex.Message}");
        }
        finally
        {
            _loading = false;
        }

        return Task.CompletedTask;
    }

    partial void OnShowUnitsInInchesChanged(bool value)
    {
        if (_loading)
        {
            return;
        }

        try
        {
            appSettings.SetShowUnitsInInches(value);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Saving the display units failed: {ex.Message}");
        }
    }

    private bool CanUpdateAllData() => !IsUpdateInProgress;

    /// <summary>Downloads the army data and, when it has changed, stores it if the user agrees.</summary>
    [RelayCommand(CanExecute = nameof(CanUpdateAllData))]
    private async Task UpdateAllDataAsync()
    {
        try
        {
            IsUpdateInProgress = true;
            UpdateStatus = "Checking for army data updates...";
            var progress = new Progress<ArmyDataProgress>(step => UpdateProgressMessage = step.Message);

            var check = await updater.CheckAsync(progress);
            UpdateStatus = await UpdateStatusAfterAsync(check, progress);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"UpdateAllDataAsync failed: {ex.Message}");
            UpdateStatus = $"Update failed: {ex.Message}";
        }
        finally
        {
            IsUpdateInProgress = false;
            UpdateProgressMessage = string.Empty;
        }
    }

    private async Task<string> UpdateStatusAfterAsync(ArmyDataCheck check, IProgress<ArmyDataProgress> progress)
    {
        if (!check.Succeeded)
        {
            return check.FailureMessage;
        }

        var unavailable = check.FailedFactions.Count == 0
            ? string.Empty
            : $" These armies couldn't be downloaded: {string.Join(", ", check.FailedFactions)}.";
        if (!check.HasUpdates)
        {
            return "The army data is up to date." + unavailable;
        }

        if (!await dialogs.ConfirmAsync(
                "Army Data Update",
                $"The Infinity Army data has changed:\n\n{check.UpdateSummary}\n\nUpdate the stored data now?",
                "Update",
                "Not Now"))
        {
            return "The stored army data was kept." + unavailable;
        }

        var notStored = await updater.ApplyAsync(check, progress);
        return (notStored.Count == 0
            ? "The army data is updated."
            : $"The army data is updated, except: {string.Join(", ", notStored)}.") + unavailable;
    }
}
