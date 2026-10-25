namespace InfinityMercsApp.Presentation.Text;

/// <summary>A stretch of <see cref="RichText"/> in one style.</summary>
/// <param name="Color">Its color; null for the text's own.</param>
/// <param name="Link">Where tapping it goes (a wiki page); null when it isn't a link.</param>
public sealed record RichTextRun(string Text, Color? Color = null, string? Link = null)
{
    /// <summary>A small gap between the entries of a list, narrower than a blank line.</summary>
    public static RichTextRun EntryGap { get; } = new(Environment.NewLine + " " + Environment.NewLine) { IsEntryGap = true };

    public bool IsEntryGap { get; private init; }
}
