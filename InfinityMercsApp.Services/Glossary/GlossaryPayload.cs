namespace InfinityMercsApp.Services.Glossary;

/// <summary>The glossary index file: its entries, in sections or as one list.</summary>
internal sealed class GlossaryPayload
{
    public List<GlossaryPayloadEntry> Entries { get; set; } = [];
    public List<GlossaryPayloadSection> Sections { get; set; } = [];
}
