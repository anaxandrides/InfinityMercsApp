namespace InfinityMercsApp.ViewModels.Components;

/// <summary>Where the detail popup's wiki description has got to.</summary>
public enum WikiStatus
{
    /// <summary>The row has no wiki page.</summary>
    None,
    Loading,

    /// <summary>The page was fetched but had nothing to show, or could not be fetched.</summary>
    Empty,
    Loaded
}
