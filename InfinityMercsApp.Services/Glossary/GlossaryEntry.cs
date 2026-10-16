namespace InfinityMercsApp.Services.Glossary;

/// <summary>A row of the rules index: a section header or a rule with its markdown file.</summary>
public sealed record GlossaryEntry(string Title, string MarkdownFile = "", bool HasDelta = false, bool IsHeader = false)
{
    public bool ShowDeltaIcon => !IsHeader && HasDelta;
}
