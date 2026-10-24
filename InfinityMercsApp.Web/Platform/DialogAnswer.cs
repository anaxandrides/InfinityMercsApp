namespace InfinityMercsApp.Web.Platform;

/// <summary>How the user answered a dialog: the button chosen (null when dismissed), and any text entered.</summary>
public sealed record DialogAnswer(string? Button, string Text)
{
    public static DialogAnswer Dismissed { get; } = new(null, string.Empty);
}
