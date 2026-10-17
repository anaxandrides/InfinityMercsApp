namespace InfinityMercsApp.ViewModels.Layouts;

/// <summary>
/// One font size for a packed row of text, such as a stat line: the largest, up to the row's usual size, at which
/// every cell's text fits its width. Text width grows in step with font size, so each cell's need at the current
/// size says what size it can take.
/// </summary>
public static class TextFit
{
    /// <param name="usualSize">The size the text has when there is room, e.g. the window's sub-headline size.</param>
    /// <param name="currentSize">The size the cells were measured at.</param>
    /// <param name="smallestSize">The size never gone below, even when the text then overflows.</param>
    public static double SizeFor(double usualSize, double currentSize, IReadOnlyList<CellText> cells, double smallestSize)
    {
        var measured = cells.Where(cell => cell.AvailableWidth > 0 && cell.NeededWidth > 0).ToList();
        if (measured.Count == 0 || currentSize <= 0)
        {
            return usualSize;
        }

        var fitting = measured.Min(cell => currentSize * cell.AvailableWidth / cell.NeededWidth);

        // Down to the half point, so the text fits rather than just touching the edge.
        return Math.Clamp(Math.Floor(fitting * 2) / 2, smallestSize, usualSize);
    }
}
