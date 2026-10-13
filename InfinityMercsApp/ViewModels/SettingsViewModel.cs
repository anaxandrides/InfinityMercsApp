using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels;

/// <summary>The display-units preference and a forced online data update.</summary>
public partial class SettingsViewModel(
    INavigationService navigationService,
    IAppSettingsProvider appSettings,
    IImportService importService) : ViewModelBase(navigationService)
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowUnitsInCentimeters))]
    private bool _showUnitsInInches = true;

    [ObservableProperty]
    private string _updateStatus = "No update run yet.";

    [ObservableProperty]
    private string _updateProgressMessage = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateAllDataCommand))]
    private bool _isUpdateInProgress;

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

    [RelayCommand(CanExecute = nameof(CanUpdateAllData))]
    private async Task UpdateAllDataAsync()
    {
        try
        {
            IsUpdateInProgress = true;
            UpdateStatus = "Running online update...";
            UpdateProgressMessage = "Starting update...";

            await foreach (var result in importService.ImportAllDataAsync(force: true))
            {
                UpdateProgressMessage = result.Message;
                UpdateStatus = result.Message;
            }
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
}
