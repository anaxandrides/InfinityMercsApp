namespace InfinityMercsApp.ViewModels;

/// <summary>A list pane's width at each split width, and the widest the detail content should read on wide screens.</summary>
public sealed record SplitPaneSizes(double MediumListWidth, double ExpandedListWidth, double WideListWidth, double WideReadingWidth);
