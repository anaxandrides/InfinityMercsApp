namespace InfinityMercsApp.Services.Glossary;

/// <summary>An entry of the glossary index: its title and markdown file.</summary>
internal sealed class GlossaryPayloadEntry
{
    public string Title { get; set; } = string.Empty;
    public string MarkdownFile { get; set; } = string.Empty;
    public bool HasDelta { get; init; }
    public string SectionHeader { get; set; } = string.Empty;
    public string Header { get; set; } = string.Empty;

    public string SectionHeaderOrHeader => string.IsNullOrWhiteSpace(SectionHeader) ? Header : SectionHeader;
}
