namespace InfinityMercsApp.Presentation.Layouts;

/// <summary>A piece of text in a packed row: the width it has, and the width it needs at its current size.</summary>
public sealed record CellText(double AvailableWidth, double NeededWidth);
