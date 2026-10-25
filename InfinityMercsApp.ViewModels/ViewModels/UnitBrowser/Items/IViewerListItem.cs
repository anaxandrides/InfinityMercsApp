namespace InfinityMercsApp.ViewModels.UnitBrowser.Items;

public interface IViewerListItem
{
    string Name { get; }

    string? CachedLogoPath { get; }

    string? PackagedLogoPath { get; }

    /// <summary>The logos to try in order; a view shows the first that loads.</summary>
    IReadOnlyList<string> LogoCandidates { get; }

    string? Subtitle { get; }

    bool HasSubtitle { get; }

    bool IsSelected { get; set; }
}
