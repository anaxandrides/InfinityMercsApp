namespace InfinityMercsApp.Services;

public sealed class WikiContentBlock
{
    public WikiBlockType Type { get; init; }
    public string Text { get; init; } = string.Empty;
    public bool Bold { get; init; }
    public int IndentLevel { get; init; }
}
