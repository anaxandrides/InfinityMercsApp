using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Presentation.Layouts;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels.Season;

/// <summary>Lists saved seasons and opens the chosen one.</summary>
public partial class LoadSeasonViewModel(
    INavigationService navigationService,
    IDialogService dialogs,
    SeasonRecordCatalog catalog) : AdaptiveViewModelBase<RecordListLayout>(navigationService, RecordListLayout.For)
{
    public ObservableCollection<SeasonRecord> SeasonRecords { get; } = [];

    [ObservableProperty]
    public partial bool HasSeasonRecords { get; set; }

    /// <summary>Reloads on every visit so newly started seasons appear.</summary>
    public override async Task InitializeAsync()
    {
        var records = await catalog.ListAsync();

        SeasonRecords.Clear();
        foreach (var record in records)
        {
            SeasonRecords.Add(record);
        }

        HasSeasonRecords = SeasonRecords.Count > 0;
    }

    [RelayCommand]
    private async Task OpenSeasonAsync(SeasonRecord? record)
    {
        if (record is null)
        {
            return;
        }

        if (!File.Exists(record.CompanyFilePath))
        {
            await dialogs.ShowAlertAsync(
                "Company Not Found",
                $"The company file for '{record.CompanyName}' could not be found. It may have been deleted.");
            return;
        }

        await NavigationService.NavigateToAsync(
            "//SeasonPage",
            NavigationParameters.ForCompany(record.CompanyFilePath, record.SeasonFilePath));
    }
}
