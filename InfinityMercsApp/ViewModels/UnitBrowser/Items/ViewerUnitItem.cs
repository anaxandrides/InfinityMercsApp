using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Services.Logos;

namespace InfinityMercsApp.ViewModels.UnitBrowser.Items;

public partial class ViewerUnitItem : ObservableObject, IViewerListItem
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Logo { get; init; }

    public string? CachedLogoPath { get; init; }

    public string? PackagedLogoPath { get; init; }

    public IReadOnlyList<string> LogoCandidates => UnitLogo.Candidates(CachedLogoPath, PackagedLogoPath);

    public string? Subtitle { get; init; }

    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

    [ObservableProperty]
    private bool _isSelected;
}
