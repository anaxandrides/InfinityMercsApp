namespace InfinityMercsApp.ViewModels;

/// <summary>A line of unit detail text and the wiki page it links to, if any.</summary>
public sealed record LinkedText(string Text, string? Url);
