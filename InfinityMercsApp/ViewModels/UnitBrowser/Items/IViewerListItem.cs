namespace InfinityMercsApp.ViewModels.UnitBrowser.Items;

public interface IViewerListItem
{
    string Name { get; }

    string? CachedLogoPath { get; }

    string? PackagedLogoPath { get; }

    string? Subtitle { get; }

    bool HasSubtitle { get; }

    bool IsSelected { get; set; }
}
