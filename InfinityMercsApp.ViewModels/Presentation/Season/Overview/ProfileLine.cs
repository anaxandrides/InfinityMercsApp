namespace InfinityMercsApp.Presentation.Season.Overview;

/// <summary>One line of a trooper's equipment, skills or weapons; a placeholder stands in for an empty list.</summary>
public sealed record ProfileLine(string Text, bool IsPlaceholder = false)
{
    public static ProfileLine None { get; } = new("(none)", IsPlaceholder: true);
}
