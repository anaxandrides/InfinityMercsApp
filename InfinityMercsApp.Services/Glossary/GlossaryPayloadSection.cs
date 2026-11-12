namespace InfinityMercsApp.Services.Glossary;

/// <summary>A section of the glossary index and its entries.</summary>
internal sealed class GlossaryPayloadSection
{
    public string Header { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<GlossaryPayloadEntry> Entries { get; set; } = [];

    public string HeaderOrTitle => string.IsNullOrWhiteSpace(Header) ? Title : Header;
}
