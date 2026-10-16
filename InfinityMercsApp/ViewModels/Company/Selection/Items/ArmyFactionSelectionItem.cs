using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;

namespace InfinityMercsApp.ViewModels.Company.Selection.Items;

public partial class ArmyFactionSelectionItem : ObservableObject, IViewerListItem, ICompanySourceFaction
{
    public int Id { get; init; }

    public int ParentId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? CachedLogoPath { get; init; }

    public string? PackagedLogoPath { get; init; }

    public string? Subtitle => null;

    public bool HasSubtitle => false;

    [ObservableProperty]
    private bool _isSelected;
}
