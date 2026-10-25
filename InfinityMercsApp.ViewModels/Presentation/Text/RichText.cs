namespace InfinityMercsApp.Presentation.Text;

/// <summary>
/// Text in styled runs (lieutenant mentions highlighted, wiki links), as a view model shows it. The MAUI app turns it
/// into a FormattedString; the web site into spans.
/// </summary>
public sealed record RichText(IReadOnlyList<RichTextRun> Runs)
{
    public static RichText Empty { get; } = new([]);

    /// <summary>The text alone, without its styling.</summary>
    public string PlainText => string.Concat(Runs.Select(run => run.Text));

    /// <summary>Text in one style.</summary>
    public static RichText Of(string text, Color? color = null) => new([new RichTextRun(text, color)]);

    public bool Equals(RichText? other) => other is not null && Runs.SequenceEqual(other.Runs);

    public override int GetHashCode() => Runs.Aggregate(0, (hash, run) => HashCode.Combine(hash, run));
}
