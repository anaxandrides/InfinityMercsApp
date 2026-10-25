using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Presentation.Layouts;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels.Company;

/// <summary>Lists saved companies to start a season with, and deletes them.</summary>
public partial class LoadCompanyViewModel(
    INavigationService navigationService,
    IDialogService dialogs,
    CompanyRecordCatalog catalog) : AdaptiveViewModelBase<RecordListLayout>(navigationService, RecordListLayout.For)
{
    public ObservableCollection<CompanyRecord> SavedRecords { get; } = [];

    [ObservableProperty]
    public partial bool HasSavedRecords { get; set; }

    /// <summary>Reloads on every visit so newly saved companies appear.</summary>
    public override Task InitializeAsync() => ReloadAsync();

    [RelayCommand]
    private async Task OpenCompanyAsync(CompanyRecord? record)
    {
        if (record is null)
        {
            return;
        }

        await NavigationService.NavigateToAsync("//SeasonPage", NavigationParameters.ForCompany(record.FilePath));
    }

    [RelayCommand]
    private async Task DeleteCompanyAsync(CompanyRecord? record)
    {
        if (record is null ||
            !await dialogs.ConfirmAsync("Delete Company", $"Confirm deletion of company [{record.CompanyName}]", "Confirm", "Back"))
        {
            return;
        }

        try
        {
            catalog.Delete(record);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await dialogs.ShowAlertAsync("Delete Failed", ex.Message);
        }

        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var records = await catalog.ListAsync();

        SavedRecords.Clear();
        foreach (var record in records)
        {
            SavedRecords.Add(record);
        }

        HasSavedRecords = SavedRecords.Count > 0;
    }
}
