using CommunityToolkit.Mvvm.ComponentModel;

namespace InfinityMercsApp.ViewModels.UnitBrowser.Items;

public partial class ViewerFactionItem : ObservableObject, IViewerListItem
{
    public int Id { get; init; }

    public int ParentId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Logo { get; init; }

    public string? CachedLogoPath { get; init; }

    public string? PackagedLogoPath { get; init; }

    public string? Subtitle => null;

    public bool HasSubtitle => false;

    [ObservableProperty]
    private bool _isSelected;
}
