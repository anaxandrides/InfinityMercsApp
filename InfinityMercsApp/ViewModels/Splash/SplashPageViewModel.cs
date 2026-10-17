using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using InfinityMercsApp.Messages;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Import;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels.Splash;

/// <summary>
/// The way into the app. Every launch downloads the army data from the Infinity Army API and compares it with
/// what is stored; changes are stored when the user accepts them (or straight away when nothing is stored yet).
/// The app opens only once there is army data to run on. When the download fails the user can retry, or carry on
/// with the stored data if there is any; there is no Exit, since iOS apps don't close themselves.
/// </summary>
public partial class SplashPageViewModel(
    IArmyDataUpdater updater,
    IDialogService dialogs,
    INavigationService navigationService) : ViewModelBase(navigationService)
{
    [ObservableProperty]
    public partial string UpdateProgressMessage { get; set; } = string.Empty;

    /// <summary>How far the current download or update has got, from 0 to 1.</summary>
    [ObservableProperty]
    public partial double UpdateProgress { get; set; }

    public override async Task InitializeAsync()
    {
        var progress = new Progress<ArmyDataProgress>(Show);
        while (!await TryLoadArmyDataAsync(progress))
        {
        }

        WeakReferenceMessenger.Default.Send(new SplashCompletedMessage());
        await NavigationService.NavigateToAsync("//MercsSeasonPage");
    }

    /// <summary>One attempt at having army data to run on; false when the user chose to try again.</summary>
    private async Task<bool> TryLoadArmyDataAsync(IProgress<ArmyDataProgress> progress)
    {
        var check = await updater.CheckAsync(progress);
        var hasStoredData = updater.HasStoredData();
        if (!check.Succeeded)
        {
            return await OfferStoredDataAsync(hasStoredData, check.FailureMessage);
        }

        if (check.FailedFactions.Count > 0 && !await dialogs.ConfirmAsync(
                "Some Armies Unavailable",
                $"These armies couldn't be downloaded: {string.Join(", ", check.FailedFactions)}.\n\n"
                + (hasStoredData ? "Their stored data will be used where there is any." : "They won't be available."),
                "Continue",
                "Retry"))
        {
            return false;
        }

        if (!check.HasUpdates)
        {
            return true;
        }

        // With nothing stored there is nothing to keep, so a first download is stored without asking.
        if (hasStoredData && !await dialogs.ConfirmAsync(
                "Army Data Update",
                $"The Infinity Army data has changed:\n\n{check.UpdateSummary}\n\nUpdate the stored data now?",
                "Update",
                "Not Now"))
        {
            return true;
        }

        return await ApplyAsync(check, progress);
    }

    private async Task<bool> ApplyAsync(ArmyDataCheck check, IProgress<ArmyDataProgress> progress)
    {
        try
        {
            var notStored = await updater.ApplyAsync(check, progress);
            return notStored.Count == 0
                   || await OfferStoredDataAsync(updater.HasStoredData(), $"These armies couldn't be stored: {string.Join(", ", notStored)}.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Storing the army data failed: {ex.Message}");
            return await OfferStoredDataAsync(updater.HasStoredData(), $"The army data couldn't be stored ({ex.Message}).");
        }
    }

    /// <summary>Carries on with the stored data if the user wants to; with none stored, the only choice is to retry.</summary>
    private async Task<bool> OfferStoredDataAsync(bool hasStoredData, string reason)
    {
        if (!hasStoredData)
        {
            await dialogs.ShowAlertAsync(
                "No Army Data",
                $"{reason}\n\nInfinity Mercs needs the Infinity Army data to run. Check your connection and try again.",
                "Retry");
            return false;
        }

        return await dialogs.ConfirmAsync(
            "Using Stored Army Data",
            $"{reason}\n\nOnly the army data stored from an earlier download is available.",
            "Continue",
            "Retry");
    }

    private void Show(ArmyDataProgress progress)
    {
        UpdateProgressMessage = progress.Message;
        UpdateProgress = progress.Fraction;
    }
}
