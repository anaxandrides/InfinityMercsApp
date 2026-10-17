using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Presentation.Company.Viewer;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.ViewModels.Base;
using InfinityMercsApp.ViewModels.Company.Viewer.Items;
using InfinityMercsApp.ViewModels.Components;

namespace InfinityMercsApp.ViewModels.Company.Viewer;

/// <summary>
/// A saved company: its troopers as a strip of tiles and the selected one in full, whose name can be
/// edited and saved back to the company file.
/// </summary>
public partial class CompanyViewerViewModel(
    INavigationService navigationService,
    ICompanyFileService companyFiles,
    IAppPackageFiles packageFiles,
    CompanyRoster roster,
    UnitDetailViewModel detail) : ViewModelBase(navigationService)
{
    public const string DefaultTitle = "Company Viewer";

    private string _companyFilePath = string.Empty;
    private string? _loadedFilePath;

    [ObservableProperty]
    public partial string Title { get; set; } = DefaultTitle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditNameCommand), nameof(SaveNameCommand))]
    public partial CompanyViewerUnitListItem? SelectedUnit { get; set; }

    [ObservableProperty]
    public partial string SelectedName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedBaseName))]
    public partial string SelectedBaseName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedUnitType))]
    public partial string SelectedUnitType { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NameDraft { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEditingName { get; set; }

    [ObservableProperty]
    public partial FormattedString RangedWeaponsFormatted { get; set; } = CompanyRoster.Simple(null, CompanyRoster.RangedColor);

    [ObservableProperty]
    public partial FormattedString MeleeWeaponsFormatted { get; set; } = CompanyRoster.Simple(null, CompanyRoster.MeleeColor);

    [ObservableProperty]
    public partial FormattedString PeripheralsFormatted { get; set; } = CompanyRoster.Simple(null, CompanyRoster.PeripheralsColor);

    [ObservableProperty]
    public partial bool HasPeripherals { get; set; }

    /// <summary>The selected trooper's stat line, icons and summaries.</summary>
    public UnitDetailViewModel Detail { get; } = detail;

    public ObservableCollection<CompanyViewerUnitListItem> CompanyUnits { get; } = [];

    public bool HasStatusMessage => StatusMessage.Length > 0;

    public bool HasSelectedBaseName => SelectedBaseName.Length > 0;

    public bool HasSelectedUnitType => SelectedUnitType.Length > 0;

    public override void ApplyQueryAttributes(IDictionary<string, object> query) =>
        _companyFilePath = NavigationParameters.GetString(query, NavigationParameters.CompanyFilePath) ?? _companyFilePath;

    /// <summary>Loads the company each time the page appears for a file it hasn't shown yet.</summary>
    public override async Task InitializeAsync()
    {
        if (_loadedFilePath != _companyFilePath)
        {
            await LoadAsync(_companyFilePath);
        }
    }

    private async Task LoadAsync(string filePath)
    {
        _loadedFilePath = filePath;
        CompanyUnits.Clear();
        ClearSelection();

        if (!companyFiles.Exists(filePath))
        {
            ShowProblem("Saved company file was not found.");
            return;
        }

        try
        {
            if (await companyFiles.LoadAsync(filePath) is not { } company)
            {
                ShowProblem("Unable to read saved company data.");
                return;
            }

            var companyName = string.IsNullOrWhiteSpace(company.CompanyName)
                ? Path.GetFileNameWithoutExtension(filePath)
                : company.CompanyName.Trim();
            Title = string.IsNullOrWhiteSpace(companyName) ? DefaultTitle : $"{DefaultTitle}: {companyName}";
            foreach (var unit in roster.UnitsOf(company))
            {
                CompanyUnits.Add(unit);
            }

            StatusMessage = CompanyUnits.Count == 0 ? "No units found in this company." : string.Empty;
            if (CompanyUnits.Count > 0)
            {
                await SelectUnitAsync(CompanyUnits[0]);
            }
        }
        catch (Exception ex)
        {
            ShowProblem($"Failed to load company: {ex.Message}");
        }
    }

    private void ShowProblem(string message)
    {
        Title = DefaultTitle;
        StatusMessage = message;
    }

    private void ClearSelection()
    {
        SelectedUnit = null;
        SelectedName = SelectedBaseName = SelectedUnitType = NameDraft = string.Empty;
        IsEditingName = false;
    }

    [RelayCommand]
    private async Task SelectUnitAsync(CompanyViewerUnitListItem? item)
    {
        if (item is null)
        {
            return;
        }

        foreach (var unit in CompanyUnits)
        {
            unit.IsSelected = ReferenceEquals(unit, item);
        }

        SelectedUnit = item;
        var profile = CompanyRoster.ProfileOf(item);
        CompanyRoster.ShowOn(Detail, item, profile);

        SelectedName = item.Name;
        NameDraft = item.Name;
        IsEditingName = false;
        var baseName = string.IsNullOrWhiteSpace(item.BaseUnitName) ? profile.Name : item.BaseUnitName;
        SelectedBaseName = string.Equals(baseName.Trim(), item.Name.Trim(), StringComparison.OrdinalIgnoreCase) ? string.Empty : baseName;
        SelectedUnitType = item.UnitTypeCode;

        Detail.HeaderColors = roster.HeaderColorsFor(item.VisualFactionId > 0 ? item.VisualFactionId : item.SourceFactionId);

        RangedWeaponsFormatted = profile.RangedWeaponsFormatted ?? CompanyRoster.Simple(profile.RangedWeapons, CompanyRoster.RangedColor);
        MeleeWeaponsFormatted = profile.MeleeWeaponsFormatted ?? CompanyRoster.Simple(profile.MeleeWeapons, CompanyRoster.MeleeColor);
        PeripheralsFormatted = profile.PeripheralsFormatted ?? CompanyRoster.Simple(profile.Peripherals, CompanyRoster.PeripheralsColor);
        HasPeripherals = profile.HasPeripherals;

        Detail.LogoSource = await CompanyRoster.LogoSourceAsync(item, packageFiles);
    }

    // ── Name editing ────────────────────────────────────────────────────────

    private bool HasSelectedUnit() => SelectedUnit is not null;

    [RelayCommand(CanExecute = nameof(HasSelectedUnit))]
    private void EditName()
    {
        NameDraft = SelectedName;
        IsEditingName = true;
    }

    [RelayCommand]
    private void RejectName()
    {
        NameDraft = SelectedName;
        IsEditingName = false;
    }

    /// <summary>Keeps the edited name and writes it to the company file; a blank name keeps the current one.</summary>
    [RelayCommand(CanExecute = nameof(HasSelectedUnit))]
    private async Task SaveNameAsync()
    {
        if (SelectedUnit is not { } unit)
        {
            return;
        }

        var name = !string.IsNullOrWhiteSpace(NameDraft) ? NameDraft.Trim()
            : !string.IsNullOrWhiteSpace(SelectedName) ? SelectedName
            : unit.IsPeripheralUnit ? FirstNonBlank(unit.BaseUnitName, "Peripheral")
            : unit.IsLieutenant ? "Captain"
            : FirstNonBlank(unit.BaseUnitName, "Unit");

        unit.Name = name;
        SelectedName = name;
        NameDraft = name;
        IsEditingName = false;
        await SaveCustomNameAsync(unit, name);
    }

    private static string FirstNonBlank(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;

    private async Task SaveCustomNameAsync(CompanyViewerUnitListItem unit, string customName)
    {
        if (!companyFiles.Exists(_companyFilePath))
        {
            return;
        }

        try
        {
            if (await companyFiles.LoadAsync(_companyFilePath) is not { } company)
            {
                return;
            }

            var entry = company.Entries.FirstOrDefault(candidate => candidate.EntryIndex == unit.EntryIndex)
                ?? company.Entries.FirstOrDefault(candidate =>
                    candidate.SourceFactionId == unit.SourceFactionId &&
                    candidate.SourceUnitId == unit.SourceUnitId &&
                    string.Equals(candidate.ProfileKey, unit.ProfileKey, StringComparison.OrdinalIgnoreCase));
            if (entry is null)
            {
                return;
            }

            entry.CustomName = customName;
            if (string.IsNullOrWhiteSpace(entry.BaseUnitName))
            {
                entry.BaseUnitName = string.IsNullOrWhiteSpace(unit.BaseUnitName) ? entry.Name : unit.BaseUnitName;
            }

            await companyFiles.SaveAsync(_companyFilePath, company);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Saving the unit's custom name failed: {ex.Message}");
        }
    }
}
